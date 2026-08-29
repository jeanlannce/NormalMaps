using BepInEx;
using BepInEx.Bootstrap;

namespace DynamicMaps.Utils
{
    // 移植自 v1.2.1 + 增强：检测可能冲突的 mod（旧版 DynamicMaps 系 / Fika / HeliCrash）
    public static class ModDetection
    {
        public static bool DynamicMapsLoaded { get; private set; }
        public static bool FikaLoaded { get; private set; }
        public static bool HeliCrashLoaded { get; private set; }

        public static void CheckForMods()
        {
            // 旧版 DynamicMaps（com.mpstark.dynamicmaps）与原版增强补丁：与 NormalMaps 抢地图界面/标记，强烈冲突
            if (Chainloader.PluginInfos.ContainsKey("com.mpstark.dynamicmaps"))
            {
                DynamicMapsLoaded = true;
                Plugin.Log.LogWarning("======================================================");
                Plugin.Log.LogWarning("检测到旧版 DynamicMaps（com.mpstark.dynamicmaps）已加载！");
                Plugin.Log.LogWarning("它与 NormalMaps 都替换地图界面并显示标记，会造成地图界面错乱、标记重复。");
                Plugin.Log.LogWarning("请卸载旧版 DynamicMaps：删除 BepInEx\\plugins\\mpstark-dynamicmaps\\ 与 SPT_Runtime\\user\\mods\\mpstark-dynamicmaps\\");
                Plugin.Log.LogWarning("======================================================");
            }

            foreach (var guid in new[] { "DynamicMapsPeekPanoramaPatch", "DynamicMapsCacheCleanerPatch" })
            {
                if (Chainloader.PluginInfos.ContainsKey(guid))
                {
                    Plugin.Log.LogWarning($"检测到原版增强补丁 {guid} 已加载，其功能已内置在 NormalMaps 中，建议删除该补丁 DLL 以避免冲突");
                }
            }

            if (Chainloader.PluginInfos.ContainsKey("com.fika.core"))
            {
                FikaLoaded = true;
                Plugin.Log.LogInfo("检测到 Fika（多人联机 mod），NormalMaps 标记功能在 Fika 下未完整验证，如有异常请反馈");
            }

            if (Chainloader.PluginInfos.ContainsKey("com.SamSWAT.HeliCrash.ArysReloaded"))
            {
                HeliCrashLoaded = true;
                Plugin.Log.LogInfo("检测到 SamSWAT HeliCrash mod，NormalMaps 内置直升机坠毁标记，二者可能重复显示，可在 F12 中关闭其一");
            }
        }
    }
}
