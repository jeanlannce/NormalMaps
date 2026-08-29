using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

// THIS IS HEAVILY BASED ON DRAKIAXYZ'S SPT-QuickMoveToContainer
namespace DynamicMaps.Config
{
    /// <summary>小地图在屏幕上的位置（v1.2.1 移植）</summary>
    public enum EMiniMapPosition
    {
        TopRight,
        BottomRight,
        TopLeft,
        BottomLeft
    }

    internal class Settings
    {
        public static ConfigFile Config;
        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public const string GeneralTitle = "1. 通用设置";
        public static ConfigEntry<bool> ReplaceMapScreen;

        public static ConfigEntry<KeyboardShortcut> CenterOnPlayerHotkey;
        public static ConfigEntry<KeyboardShortcut> DumpInfoHotkey;

        public static ConfigEntry<KeyboardShortcut> MoveMapUpHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapDownHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapLeftHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapRightHotkey;
        public static ConfigEntry<float> MapMoveHotkeySpeed;
        public static ConfigEntry<bool> MoveMapYInverted;

        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelUpHotkey;
        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelDownHotkey;

        public static ConfigEntry<KeyboardShortcut> ZoomMapInHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomMapOutHotkey;
        public static ConfigEntry<float> ZoomMapHotkeySpeed;

        public const string DynamicMarkerTitle = "2. 动态标记";
        public static ConfigEntry<bool> ShowPlayerMarker;

        public static ConfigEntry<bool> ShowFriendlyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowEnemyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowScavMarkersInRaid;
        public static ConfigEntry<bool> ShowBossMarkersInRaid;

        public static ConfigEntry<bool> ShowLockedDoorStatus;

        public static ConfigEntry<bool> ShowQuestsInRaid;

        public static ConfigEntry<bool> ShowExtractsInRaid;
        public static ConfigEntry<bool> ShowExtractStatusInRaid;

        public static ConfigEntry<bool> ShowDroppedBackpackInRaid;

        public static ConfigEntry<bool> ShowBTRInRaid;

        public static ConfigEntry<bool> ShowAirdropsInRaid;

        // —— v1.2.1 移植：愿望清单 / 隐藏仓库 / 转运点 / 秘密撤离点 / 直升机坠毁 ——
        public static ConfigEntry<bool> ShowWishListItemsInRaid;
        public static ConfigEntry<bool> ShowHiddenStashesInRaid;
        public static ConfigEntry<bool> ShowTransitPointsInRaid;
        public static ConfigEntry<bool> ShowSecretPointsInRaid;
        public static ConfigEntry<bool> ShowHeliCrashMarker;

        public static ConfigEntry<int> ShowWishListItemsIntelLevel;
        public static ConfigEntry<int> ShowHiddenStashIntelLevel;

        public const string MarkerColorTitle = "4. 标记颜色";
        public static ConfigEntry<Color> LootItemColor;
        public static ConfigEntry<Color> SecretPointColor;
        public static ConfigEntry<Color> HiddenStashColor;
        public static ConfigEntry<Color> TransPointColor;
        public static ConfigEntry<Color> AirdropColor;

        public static ConfigEntry<bool> ShowFriendlyCorpsesInRaid;
        public static ConfigEntry<bool> ShowKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowFriendlyKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowBossCorpsesInRaid;
        public static ConfigEntry<bool> ShowOtherCorpsesInRaid;

        public const string InRaidTitle = "3. 战局内";
        public static ConfigEntry<bool> ResetZoomOnCenter;
        public static ConfigEntry<float> CenteringZoomResetPoint;

        public static ConfigEntry<bool> AutoCenterOnPlayerMarker;
        public static ConfigEntry<bool> AutoSelectLevel;

        public static ConfigEntry<KeyboardShortcut> PeekShortcut;
        public static ConfigEntry<bool> HoldForPeek;
        public static ConfigEntry<float> PeekZoomScale;

        // —— v1.2.1 移植：小地图（MiniMap）——
        public const string MiniMapTitle = "5. 小地图";
        public static ConfigEntry<bool> MiniMapEnabled;
        public static ConfigEntry<KeyboardShortcut> MiniMapShowOrHide;
        public static ConfigEntry<EMiniMapPosition> MiniMapPosition;
        public static ConfigEntry<float> MiniMapSizeX;
        public static ConfigEntry<float> MiniMapSizeY;
        public static ConfigEntry<float> MiniMapScreenOffsetX;
        public static ConfigEntry<float> MiniMapScreenOffsetY;
        public static ConfigEntry<float> ZoomMiniMap;
        public static ConfigEntry<float> ZoomMainMap;
        public static ConfigEntry<KeyboardShortcut> ZoomInMiniMapHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomOutMiniMapHotkey;

        /// <summary>主地图缩放值（normalized 0-1）变更事件</summary>
        public static event Action<float> OnZoomMainMapChanged;
        /// <summary>小地图缩放值（normalized 0-1）变更事件</summary>
        public static event Action<float> OnZoomMiniMapChanged;

        // public static ConfigEntry<KeyboardShortcut> KeyboardShortcut;

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(ReplaceMapScreen = Config.Bind(
                GeneralTitle,
                "替换游戏地图界面",
                true,
                new ConfigDescription(
                    "是否替换游戏默认地图界面（切换地图后需重新打开生效）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(CenterOnPlayerHotkey = Config.Bind(
                GeneralTitle,
                "居中到玩家热键",
                new KeyboardShortcut(KeyCode.Semicolon),
                new ConfigDescription(
                    "地图打开时按下，将地图居中到玩家位置",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapUpHotkey = Config.Bind(
                GeneralTitle,
                "地图上移热键",
                new KeyboardShortcut(KeyCode.UpArrow),
                new ConfigDescription(
                    "上移地图的热键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapDownHotkey = Config.Bind(
                GeneralTitle,
                "地图下移热键",
                new KeyboardShortcut(KeyCode.DownArrow),
                new ConfigDescription(
                    "下移地图的热键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapLeftHotkey = Config.Bind(
                GeneralTitle,
                "地图左移热键",
                new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    "左移地图的热键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapRightHotkey = Config.Bind(
                GeneralTitle,
                "地图右移热键",
                new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    "右移地图的热键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MapMoveHotkeySpeed = Config.Bind(
                GeneralTitle,
                "地图移动速度",
                0.25f,
                new ConfigDescription(
                    "地图移动速度（每秒移动地图的百分比）",
                    new AcceptableValueRange<float>(0.05f, 2f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MoveMapYInverted = Config.Bind(
                GeneralTitle,
                "方向键移动大地图Y轴反转",
                false,
                new ConfigDescription(
                    "开启后方向键上/下移动大地图时 Y 轴方向反转",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ChangeMapLevelUpHotkey = Config.Bind(
                GeneralTitle,
                "向上切换图层热键",
                new KeyboardShortcut(KeyCode.Period),
                new ConfigDescription(
                    "向上切换地图图层的热键（地图界面中 Shift+滚轮上 同样生效）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ChangeMapLevelDownHotkey = Config.Bind(
                GeneralTitle,
                "向下切换图层热键",
                new KeyboardShortcut(KeyCode.Comma),
                new ConfigDescription(
                    "向下切换地图图层的热键（地图界面中 Shift+滚轮下 同样生效）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapInHotkey = Config.Bind(
                GeneralTitle,
                "放大地图热键",
                new KeyboardShortcut(KeyCode.Keypad8),
                new ConfigDescription(
                    "放大地图的热键（地图界面中滚轮上 同样生效）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapOutHotkey = Config.Bind(
                GeneralTitle,
                "缩小地图热键",
                new KeyboardShortcut(KeyCode.Keypad5),
                new ConfigDescription(
                    "缩小地图的热键（地图界面中滚轮下 同样生效）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMapHotkeySpeed = Config.Bind(
                GeneralTitle,
                "热键缩放速度",
                2.5f,
                new ConfigDescription(
                    "热键缩放地图的速率",
                    new AcceptableValueRange<float>(1f, 10f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(DumpInfoHotkey = Config.Bind(
                GeneralTitle,
                "导出信息热键",
                new KeyboardShortcut(KeyCode.D, KeyCode.LeftShift, KeyCode.LeftAlt),
                new ConfigDescription(
                    "地图打开时按下，将撤离点/战利品/开关的标记数据导出为 json 到插件目录",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true })));

            ConfigEntries.Add(ShowPlayerMarker = Config.Bind(
                DynamicMarkerTitle,
                "显示玩家标记",
                true,
                new ConfigDescription(
                    "是否在战局内显示玩家标记",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyPlayerMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示友方玩家标记",
                true,
                new ConfigDescription(
                    "是否在战局内显示友方玩家标记",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowEnemyPlayerMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示敌方玩家标记",
                true,
                new ConfigDescription(
                    "是否在战局内显示敌方玩家标记（通常用于调试）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowScavMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示 Scav 标记",
                true,
                new ConfigDescription(
                    "是否在战局内显示敌方 Scav 标记（通常用于调试）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBossMarkersInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示 Boss 标记",
                true,
                new ConfigDescription(
                    "是否在战局内显示敌方 Boss 标记",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowLockedDoorStatus = Config.Bind(
                DynamicMarkerTitle,
                "显示上锁门状态",
                true,
                new ConfigDescription(
                    "是否根据钥匙获取情况更新上锁门标记状态",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowQuestsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示任务",
                true,
                new ConfigDescription(
                    "是否在战局内显示任务",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowExtractsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示撤离点",
                true,
                new ConfigDescription(
                    "是否在战局内显示撤离点",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowExtractStatusInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示撤离点状态",
                true,
                new ConfigDescription(
                    "是否根据撤离点状态着色",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowDroppedBackpackInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示丢弃背包",
                true,
                new ConfigDescription(
                    "是否显示玩家本人丢弃的背包（不包括他人）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBTRInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示 BTR",
                true,
                new ConfigDescription(
                    "是否在战局内显示 BTR",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowAirdropsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示空投",
                true,
                new ConfigDescription(
                    "是否在空投落地时显示空投标记",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowWishListItemsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示愿望清单物品",
                true,
                new ConfigDescription(
                    "是否在战局内显示愿望清单中的物品",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowHiddenStashesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示隐藏仓库",
                true,
                new ConfigDescription(
                    "是否在战局内显示隐藏仓库",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowTransitPointsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示转运点",
                true,
                new ConfigDescription(
                    "是否在战局内显示转运点",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowSecretPointsInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示秘密撤离点",
                true,
                new ConfigDescription(
                    "是否在战局内显示秘密撤离点",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowHeliCrashMarker = Config.Bind(
                DynamicMarkerTitle,
                "显示直升机坠毁点",
                true,
                new ConfigDescription(
                    "是否在战局内标记直升机坠毁点",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowWishListItemsIntelLevel = Config.Bind(
                InRaidTitle,
                "愿望清单物品情报等级",
                0,
                new ConfigDescription(
                    "情报等级达到该值时显示愿望清单物品（0-3）",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowHiddenStashIntelLevel = Config.Bind(
                InRaidTitle,
                "隐藏仓库情报等级",
                0,
                new ConfigDescription(
                    "情报等级达到该值时显示隐藏仓库（0-3）",
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(LootItemColor = Config.Bind(
                MarkerColorTitle,
                "战利品标记颜色",
                new Color(0.98f, 0.81f, 0.007f),
                new ConfigDescription(
                    "战利品标记的颜色",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(SecretPointColor = Config.Bind(
                MarkerColorTitle,
                "秘密点标记颜色",
                new Color(0.1f, 0.6f, 0.6f),
                new ConfigDescription(
                    "秘密撤离点标记的颜色",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(HiddenStashColor = Config.Bind(
                MarkerColorTitle,
                "隐藏仓库标记颜色",
                new Color(1f, 0.92f, 0.01f),
                new ConfigDescription(
                    "隐藏仓库标记的颜色",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(TransPointColor = Config.Bind(
                MarkerColorTitle,
                "转运点标记颜色",
                new Color(1f, 0.62f, 0.2f),
                new ConfigDescription(
                    "转运点标记的颜色",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AirdropColor = Config.Bind(
                MarkerColorTitle,
                "空投标记颜色",
                new Color(1f, 0.3f, 0.01f),
                new ConfigDescription(
                    "空投标记的颜色",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "战局内显示友方尸体",
                true,
                new ConfigDescription(
                    "是否在战局内显示友方尸体",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowKilledCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示玩家击杀的尸体",
                true,
                new ConfigDescription(
                    "是否显示被玩家击杀的尸体（击杀的 Boss 会用另一种颜色显示）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowFriendlyKilledCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示友方击杀的尸体",
                true,
                new ConfigDescription(
                    "是否显示被友方击杀的尸体（击杀的 Boss 会用另一种颜色显示）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowBossCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示 Boss 尸体",
                false,
                new ConfigDescription(
                    "是否显示 Boss 尸体（玩家击杀的除外）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ShowOtherCorpsesInRaid = Config.Bind(
                DynamicMarkerTitle,
                "显示其他尸体",
                false,
                new ConfigDescription(
                    "是否显示其他尸体（友方或玩家击杀的除外）",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AutoSelectLevel = Config.Bind(
                InRaidTitle,
                "自动选择图层",
                true,
                new ConfigDescription(
                    "是否根据玩家在战局中的位置自动选择地图图层",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(AutoCenterOnPlayerMarker = Config.Bind(
                InRaidTitle,
                "自动居中到玩家标记",
                true,
                new ConfigDescription(
                    "是否在战局中显示地图时将玩家标记居中",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ResetZoomOnCenter = Config.Bind(
                InRaidTitle,
                "居中时重置缩放",
                true,
                new ConfigDescription(
                    "是否在战局内每次打开地图时重置缩放级别",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(CenteringZoomResetPoint = Config.Bind(
                InRaidTitle,
                "居中时的缩放级别",
                0.15f,
                new ConfigDescription(
                    "居中到玩家时使用的缩放级别（0 为完全缩小，1 为完全放大）",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PeekShortcut = Config.Bind(
                InRaidTitle,
                "窥视地图快捷键",
                new KeyboardShortcut(KeyCode.M),
                new ConfigDescription(
                    "窥视地图的快捷键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(HoldForPeek = Config.Bind(
                InRaidTitle,
                "按住窥视",
                true,
                new ConfigDescription(
                    "是否按住快捷键保持打开；关闭后为单击切换",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(PeekZoomScale = Config.Bind(
                InRaidTitle,
                "窥视全景缩放倍率",
                1.0f,
                new ConfigDescription(
                    "按窥视键查看全景时的缩放倍率（1.0 = 完整地图刚好填满屏幕；调大放大、调小缩小）",
                    new AcceptableValueRange<float>(0.5f, 2f),
                    new ConfigurationManagerAttributes { })));

            // —— v1.2.1 移植：小地图（MiniMap）——
            ConfigEntries.Add(MiniMapEnabled = Config.Bind(
                MiniMapTitle,
                "启用小地图",
                true,
                new ConfigDescription(
                    "是否在战局内屏幕角落显示小地图",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MiniMapPosition = Config.Bind(
                MiniMapTitle,
                "小地图位置",
                EMiniMapPosition.TopRight,
                new ConfigDescription(
                    "小地图显示在屏幕的哪个角落",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MiniMapSizeX = Config.Bind(
                MiniMapTitle,
                "小地图水平尺寸",
                275f,
                new ConfigDescription(
                    "小地图的宽度",
                    new AcceptableValueRange<float>(0f, 850f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MiniMapSizeY = Config.Bind(
                MiniMapTitle,
                "小地图垂直尺寸",
                275f,
                new ConfigDescription(
                    "小地图的高度",
                    new AcceptableValueRange<float>(0f, 850f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MiniMapScreenOffsetX = Config.Bind(
                MiniMapTitle,
                "小地图水平偏移",
                0f,
                new ConfigDescription(
                    "小地图距屏幕边缘的水平偏移（按分辨率缩放，改分辨率需重启）",
                    new AcceptableValueRange<float>(-640f, 2560f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MiniMapScreenOffsetY = Config.Bind(
                MiniMapTitle,
                "小地图垂直偏移",
                0f,
                new ConfigDescription(
                    "小地图距屏幕边缘的垂直偏移（按分辨率缩放，改分辨率需重启）",
                    new AcceptableValueRange<float>(-360f, 1440f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(MiniMapShowOrHide = Config.Bind(
                MiniMapTitle,
                "显示或隐藏小地图热键",
                new KeyboardShortcut(KeyCode.End),
                new ConfigDescription(
                    "在战局内按此热键切换小地图显示/隐藏",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomInMiniMapHotkey = Config.Bind(
                MiniMapTitle,
                "小地图放大热键",
                new KeyboardShortcut(KeyCode.Keypad8),
                new ConfigDescription(
                    "小地图显示时放大小地图的热键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomOutMiniMapHotkey = Config.Bind(
                MiniMapTitle,
                "小地图缩小热键",
                new KeyboardShortcut(KeyCode.Keypad5),
                new ConfigDescription(
                    "小地图显示时缩小地图的热键",
                    null,
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMiniMap = Config.Bind(
                MiniMapTitle,
                "小地图缩放级别",
                0.33f,
                new ConfigDescription(
                    "小地图的默认缩放级别（0 完全缩小，1 完全放大）",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { })));

            ConfigEntries.Add(ZoomMainMap = Config.Bind(
                InRaidTitle,
                "主地图缩放级别",
                0f,
                new ConfigDescription(
                    "主地图（Tab 视图/peek 视图）的缩放级别（0 完全缩小，1 完全放大）",
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { })));

            // 缩放值变更事件（供 MapView 独立应用主/小地图缩放）
            ZoomMainMap.SettingChanged += (s, e) => OnZoomMainMapChanged?.Invoke(ZoomMainMap.Value);
            ZoomMiniMap.SettingChanged += (s, e) => OnZoomMiniMapChanged?.Invoke(ZoomMiniMap.Value);

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                var attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
