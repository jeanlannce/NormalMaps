using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.UI;
using DynamicMaps.Utils;
using EFT.UI;
using EFT.UI.Map;

namespace DynamicMaps
{
    // NormalMaps —— 原版 DynamicMaps 0.3.4 的独立复现版（作者: jeanlannce，MIT 版权归原作者 mpstark）
// the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.jeanlannce.normalmaps", "NormalMaps", BuildInfo.Version)]
    [BepInDependency("com.SPT.custom")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 40743;  // SPT 4.1.3
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Logger;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ModdedMapScreen Map;

        internal void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            Settings.Init(Config);
            Config.SettingChanged += (x, y) => Map?.ReadConfig();

            Instance = this;

            // 检测冲突 mod（旧版 DynamicMaps / Fika / HeliCrash）
            ModDetection.CheckForMods();

            // patches
            new BattleUIScreenShowPatch().Enable();
            new CommonUIAwakePatch().Enable();
            new MapScreenShowPatch().Enable();
            new MapScreenClosePatch().Enable();
            new GameWorldOnDestroyPatch().Enable();
            new GameStartedPatch().Enable();
            new GameWorldUnregisterPlayerPatch().Enable();
            new GameWorldRegisterLootItemPatch().Enable();
            new GameWorldDestroyLootPatch().Enable();
            new AirdropBoxOnBoxLandPatch().Enable();
            new PlayerOnDeadPatch().Enable();
            new PlayerInventoryThrowItemPatch().Enable();
        }

        /// <summary>
        /// Attach to the map screen
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen mapScreen)
        {
            if (Map != null)
            {
                return;
            }

            Log.LogInfo("Trying to attach to MapScreen");

            // attach to common UI first to call awake and set things up, then attach to sleeping map screen
            Map = ModdedMapScreen.Create(Singleton<CommonUI>.Instance.gameObject);
            Map.transform.SetParent(mapScreen.transform);
        }

        /// <summary>
        /// Attach the peek component
        /// </summary>
        internal void TryAttachToBattleUIScreen(EftBattleUIScreen battleUI)
        {
            if (Map == null)
            {
                return;
            }

            Map.TryAddPeekComponent(battleUI);
        }
    }
}
