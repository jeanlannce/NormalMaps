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

        // 惰性初始化（Unity.VectorGraphics 反射链路）；初始化失败时降级为“图层空白”，避免反复抛异常
        private static bool _initialized;
        private static bool _initFailed;
        private static bool _warnedInitFailed;

        private static object[] TesselationIndex;
        private static System.Reflection.MethodInfo _importSvgMethod;
        private static System.Reflection.MethodInfo _tessellateSceneMethod;
        private static System.Reflection.MethodInfo _buildSpriteMethod;
        private static System.Reflection.FieldInfo _geometryVerticesField;

        public static Sprite GetOrLoadCachedSprite(MapLayerDef def)
        {
            EnsureInitialized();
            if (_initFailed)
            {
                if (!_warnedInitFailed)
                {
                    _warnedInitFailed = true;
                    Plugin.Log.LogWarning("[SvgUtils] SVG rendering unavailable, map layers will be blank (markers still show)");
                }

                return null;
            }

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

        /// <summary>
        /// 惰性执行反射初始化。Unity.VectorGraphics.dll 缺失/不兼容时给出明确指引，
        /// 而不是让 TypeInitializationException 污染所有图层加载路径。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized || _initFailed)
            {
                return;
            }

            try
            {
                // 不能用 Type.GetType("...")——Unity.VectorGraphics 程序集可能尚未被运行时加载，GetType 会返回 null
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

                _initialized = true;
            }
            catch (Exception e)
            {
                _initFailed = true;
                Plugin.Log.LogError("[SvgUtils] Unity.VectorGraphics 初始化失败！SVG 地图图层将无法渲染（标记图标仍可显示）。");
                Plugin.Log.LogError("[SvgUtils] 请确认游戏 EscapeFromTarkov_Data/Managed/ 目录包含：Unity.VectorGraphics.dll、Unity.Mathematics.dll、Unity.Collections.dll、Unity.Collections.LowLevel.ILSupport.dll");
                LogExceptionChain(e);
            }
        }

        private static void LogExceptionChain(Exception e)
        {
            var indent = "";
            while (e != null)
            {
                Plugin.Log.LogError($"[SvgUtils] {indent}{e.GetType().Name}: {e.Message}");
                e = e.InnerException;
                indent += "  -> ";
            }
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
                    if (sprite != null)
                    {
                        // 注意：纯色 SVG 的 sprite.texture 可能为 null（GenerateAtlas 对 SolidFill 返回 null，属正常）
                        // SVGImage 用顶点颜色渲染不依赖 texture——只在真正失败（null sprite）时打警告
                        var texInfo = sprite.texture != null ? $"{sprite.texture.width}x{sprite.texture.height}" : "vector(no atlas)";
                        Plugin.Log.LogInfo($"[SvgUtils] SVG layer rendered: {Path.GetFileName(absolutePath)} ({texInfo})");
                    }
                    return sprite;
                }

                Plugin.Log.LogWarning($"[SvgUtils] All presets failed for {absolutePath}, layer will be blank");
                return null;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[SvgUtils] Failed to render {absolutePath}: {e.GetType().Name}: {e.Message}");
                LogExceptionChain(e.InnerException);
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
    }
}
