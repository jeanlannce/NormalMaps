using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DynamicMaps.Utils
{
    /// <summary>
    /// 本地化层：内置中文翻译，可选读取 BepInEx/plugins/zh-cn/dynamicmaps.json 覆盖（兼容 zh-cn 格式）。
    /// 格式与 ConfigurationManager 汉化 ui.json 一致：{ "英文原文": "中文译文" }
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, string> _builtin = new Dictionary<string, string>
        {
            { "Select a Map", "选择地图" },
            { "Level", "图层" },
            { "Cursor:", "光标:" },
            { "Player:", "玩家:" },
        };

        private static Dictionary<string, string> _overrides;

        static Localization()
        {
            try
            {
                // Plugin.Path = BepInEx/plugins/NormalMaps/ → ../zh-cn/dynamicmaps.json = BepInEx/plugins/zh-cn/dynamicmaps.json
                var path = Path.Combine(Plugin.Path, "..", "zh-cn", "dynamicmaps.json");
                if (File.Exists(path))
                {
                    _overrides = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(path));
                    Plugin.Log?.LogInfo($"[NormalMaps] 已加载外部汉化覆盖: {path}");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log?.LogWarning($"[NormalMaps] 加载 zh-cn/dynamicmaps.json 失败: {e.Message}");
            }
        }

        /// <summary>取翻译：外部覆盖 > 内置中文 > 原文</summary>
        public static string Get(string key)
        {
            if (_overrides != null && _overrides.TryGetValue(key, out var v))
            {
                return v;
            }
            if (_builtin.TryGetValue(key, out var z))
            {
                return z;
            }
            return key;
        }
    }
}
