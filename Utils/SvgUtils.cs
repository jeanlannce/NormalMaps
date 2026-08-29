using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicMaps.Data;
using UnityEngine;

namespace DynamicMaps.Utils
{
    /// <summary>
    /// 移植自 v1.2.1：SVG 图层渲染（TarkovDev 系列地图图层为 .svg）。
    /// TessellationOptions 是 Unity.VectorGraphics 的 internal 类型，源码不可直接引用 → 全反射实现。
    /// </summary>
    public static class SvgUtils
    {
        private static readonly Dictionary<(string, int), Sprite> MapCache = new Dictionary<(string, int), Sprite>();

        private static readonly Regex ViewBoxRegex = new Regex("<svg[^>]*\\sviewBox=\"([^\"]+)\"", RegexOptions.Compiled);

        private static readonly object[] TesselationIndex;
        private static readonly System.Reflection.MethodInfo _importSvgMethod;
        private static readonly System.Reflection.MethodInfo _tessellateSceneMethod;
        private static readonly System.Reflection.MethodInfo _buildSpriteMethod;
        private static readonly System.Reflection.FieldInfo _geometryVerticesField;

        public static Sprite GetOrLoadCachedSprite(MapLayerDef def)
        {
            var key = (def.ImagePath, def.TesselationIndex);
            if (MapCache.TryGetValue(key, out var value))
            {
                return value;
            }

            var absolutePath = Path.Combine(Plugin.Path, def.ImagePath);
            return MapCache[key] = LoadSvgFromPath(def, absolutePath);
        }

        public static void ClearCache()
        {
            MapCache.Clear();
        }

        private static Sprite LoadSvgFromPath(MapLayerDef def, string absolutePath)
        {
            try
            {
                var text = File.ReadAllText(absolutePath);
                var viewbox = GetViewbox(text);
                if (!viewbox.HasValue)
                {
                    Plugin.Log.LogWarning($"[SvgUtils] No viewBox found in {absolutePath}, layer will be blank");
                    return null;
                }

                using var stringReader = new StringReader(text);
                // 4.1.3 签名（反射验证）：ImportSVG 有两个重载，v1.2.1 用的是带 ViewportOptions 的重载：
                //   (TextReader, ViewportOptions, float dpi, float pixelsPerUnit, int windowWidth, int windowHeight)
                //   v1.2.1 参数 (reader, (ViewportOptions)2, 0f, 1f, 0, 0)
                var viewportOptionsType = _importSvgMethod.GetParameters()[1].ParameterType;
                var viewportOptions = Enum.ToObject(viewportOptionsType, 2);
                var sceneInfo = _importSvgMethod.Invoke(null, new object[] { stringReader, viewportOptions, 0f, 1f, 0, 0 });
                var scene = sceneInfo.GetType().GetProperty("Scene").GetValue(sceneInfo);

                // 4.1.3 签名：TessellateScene(Scene, TessellationOptions, Dictionary<SceneNode,float> nodeOpacities)——传 null 即可
                for (int i = Mathf.Clamp(def.TesselationIndex, 0, TesselationIndex.Length - 1); i < TesselationIndex.Length; i++)
                {
                    var list = (IList)_tessellateSceneMethod.Invoke(null, new object[] { scene, TesselationIndex[i], null });
                    if (list == null || list.Count == 0)
                    {
                        Plugin.Log.LogWarning($"[SvgUtils] Preset {i} produced empty geometry for {absolutePath}, trying next.");
                        continue;
                    }

                    if (OverBudgetVertices(list))
                    {
                        Plugin.Log.LogWarning($"[SvgUtils] Preset {i} over budget for {absolutePath}, trying next.");
                        continue;
                    }

                    // 4.1.3 签名：BuildSprite(List<Geometry>, Rect, float svgPixelsPerUnit, Alignment, Vector2 customPivot, ushort gradientResolution, bool flipYAxis)
                    var alignment = Enum.ToObject(_buildSpriteMethod.GetParameters()[3].ParameterType, 0);
                    var sprite = (Sprite)_buildSpriteMethod.Invoke(null, new object[] { list, viewbox.Value, 1f, alignment, Vector2.zero, (ushort)32, true });
                    if (sprite != null && sprite.texture != null)
                    {
                        Plugin.Log.LogInfo($"[SvgUtils] SVG layer rendered: {Path.GetFileName(absolutePath)} ({sprite.texture.width}x{sprite.texture.height})");
                    }
                    return sprite;
                }

                Plugin.Log.LogWarning($"[SvgUtils] All presets failed for {absolutePath}, layer will be blank");
                return null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[SvgUtils] Failed to render {absolutePath}: {e.Message}");
                return null;
            }
        }

        private static bool OverBudgetVertices(IList geometry)
        {
            var num = 0;
            foreach (var item in geometry)
            {
                var vertices = (Vector2[])_geometryVerticesField.GetValue(item);
                num += vertices?.Length ?? 0;
                if (num > 65500)
                {
                    return true;
                }
            }

            return false;
        }

        private static Rect? GetViewbox(string svgText)
        {
            var match = ViewBoxRegex.Match(svgText);
            if (!match.Success)
            {
                return null;
            }

            var parts = match.Groups[1].Value.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                return null;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
             || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
             || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)
             || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                return null;
            }

            if (w <= 0f || h <= 0f)
            {
                return null;
            }

            return new Rect(x, y, w, h);
        }

        private static object MakeTessellationOptions(Type t, float step, float cord, float tan, float sampling)
        {
            // 注意：这些是 TessellationOptions 的 public 属性（internal struct），GetField 找不到（字段是 internal 的 m_xxx）
            var o = Activator.CreateInstance(t);
            t.GetProperty("StepDistance").SetValue(o, step);
            t.GetProperty("MaxCordDeviation").SetValue(o, cord);
            t.GetProperty("MaxTanAngleDeviation").SetValue(o, tan);
            t.GetProperty("SamplingStepSize").SetValue(o, sampling);
            return o;
        }

        static SvgUtils()
        {
            // 注意：不能用 Type.GetType("...")——Unity.VectorGraphics 程序集可能尚未被运行时加载，GetType 会返回 null
            // 用编译期 typeof 引用程序集后从 Assembly 查类型，保证稳定解析
            var utilsType = typeof(Unity.VectorGraphics.VectorUtils);
            var tessType = utilsType.Assembly.GetType("Unity.VectorGraphics.VectorUtils+TessellationOptions");

            TesselationIndex = new object[5];
            TesselationIndex[0] = MakeTessellationOptions(tessType, 1.5f, 0.2f, 0.2f, 0.04f);
            TesselationIndex[1] = MakeTessellationOptions(tessType, 2f, 0.3f, 0.25f, 0.05f);
            TesselationIndex[2] = MakeTessellationOptions(tessType, 4f, 0.4f, 0.3f, 0.06f);
            TesselationIndex[3] = MakeTessellationOptions(tessType, 6f, 0.5f, 0.4f, 0.07f);
            TesselationIndex[4] = MakeTessellationOptions(tessType, 8f, 0.6f, 0.5f, 0.08f);

            _importSvgMethod = typeof(Unity.VectorGraphics.SVGParser).GetMethods()
                .First(m => m.Name == "ImportSVG" && m.GetParameters().Length == 6
                    && m.GetParameters()[1].ParameterType.Name == "ViewportOptions");
            _tessellateSceneMethod = utilsType.GetMethods()
                .First(m => m.Name == "TessellateScene" && m.GetParameters().Length == 3);
            _buildSpriteMethod = utilsType.GetMethods()
                .First(m => m.Name == "BuildSprite" && m.GetParameters().Length == 7);
            _geometryVerticesField = utilsType.Assembly.GetType("Unity.VectorGraphics.VectorUtils+Geometry")
                .GetField("Vertices");
        }
    }
}
