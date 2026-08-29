using BepInEx.Configuration;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        public RectTransform MapScreenTrueParent { get; set; }

        public RectTransform RectTransform { get; private set; }
        public KeyboardShortcut PeekShortcut { get; set; }
        public bool HoldForPeek { get; set; }  // opposite is peek toggle
        public bool IsPeeking { get; private set; }

        // —— v1.2.1 移植：小地图（MiniMap）——
        public KeyboardShortcut HideMinimapShortcut { get; set; }
        private bool IsMiniMapHidden = false;
        public bool ShowingMiniMap { get; private set; }
        public bool WasMiniMapActive { get; set; }

        internal static MapPeekComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            go.GetRectTransform().sizeDelta = parent.GetRectTransform().sizeDelta;

            var component = go.AddComponent<MapPeekComponent>();

            return component;
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
        }

        private void Update()
        {
            if (!GameUtils.ShouldShowMapInRaid())
            {
                if (ShowingMiniMap)
                {
                    EndMiniMap();
                }

                if (IsPeeking)
                {
                    EndPeek();
                }
            }
            else
            {
                HandleMinimapState();
                HandlePeekState();
            }
        }

        // —— v1.2.1 移植：小地图状态机 ——
        private static bool IsMiniMapEnabled => Settings.MiniMapEnabled.Value;

        private void HandleMinimapState()
        {
            if (!IsMiniMapEnabled)
            {
                if (ShowingMiniMap)
                {
                    EndMiniMap();
                    WasMiniMapActive = false;
                }

                return;
            }

            if (HideMinimapShortcut.BetterIsDown())
            {
                // toggle minimap hidden state
                IsMiniMapHidden = !IsMiniMapHidden;
                if (!IsMiniMapHidden && !ShowingMiniMap)
                {
                    BeginMiniMap(false);
                }
                else
                {
                    EndMiniMap();
                }
            }
            else if (!IsMiniMapHidden)
            {
                // show minimap when not peeking and not showing the full map screen
                if (!IsPeeking && !MapScreen.IsShowingMapScreen)
                {
                    BeginMiniMap();
                }
                else
                {
                    EndMiniMap();
                }
            }
        }

        private void HandlePeekState()
        {
            if (HoldForPeek && PeekShortcut.BetterIsPressed() != IsPeeking)
            {
                // hold for peek
                if (PeekShortcut.BetterIsPressed())
                {
                    WasMiniMapActive = ShowingMiniMap;
                    MapScreen.SaveMainMapPos();
                    EndMiniMap();
                    BeginPeek(WasMiniMapActive && !IsMiniMapHidden);
                }
                else
                {
                    EndPeek();
                }
            }
            else if (!HoldForPeek && PeekShortcut.BetterIsDown())
            {
                // toggle peek
                if (!IsPeeking)
                {
                    WasMiniMapActive = ShowingMiniMap;
                    MapScreen.SaveMainMapPos();
                    EndMiniMap();
                    BeginPeek(WasMiniMapActive);
                }
                else
                {
                    EndPeek();
                }
            }
        }

        internal void BeginPeek(bool playAnimation = true)
        {
            if (IsPeeking)
            {
                return;
            }

            // just in case something else is attached and tries to be in front
            transform.SetAsLastSibling();

            IsPeeking = true;

            // attach map screen to peek mask
            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show(playAnimation);

            // NormalMaps 增强：peek 全景观测（缩到最小缩放 + 地图中心对准画面中心）
            MapScreen.FullMapPanorama();
        }

        internal void EndPeek()
        {
            if (!IsPeeking)
            {
                return;
            }

            IsPeeking = false;

            // un-attach map screen and re-attach to true parent
            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);

            // NormalMaps 增强：松开 M 后恢复默认状态（缩放 + 跟随玩家）
            MapScreen.RestoreDefaultState();

            // v1.2.1 移植：peek 前小地图在显示，则恢复小地图
            if (WasMiniMapActive)
            {
                BeginMiniMap();
            }
        }

        internal void BeginMiniMap(bool playAnimation = true)
        {
            if (ShowingMiniMap)
            {
                return;
            }

            ShowingMiniMap = true;
            MapScreen.MapView.IsMiniMapActive = true;
            MapScreen.MapView.ApplyMiniMapZoom();

            // attach map screen to peek mask (full-screen, mask handles the corner size)
            transform.SetAsLastSibling();
            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show(playAnimation);
        }

        internal void EndMiniMap()
        {
            if (!ShowingMiniMap)
            {
                return;
            }

            ShowingMiniMap = false;
            MapScreen.MapView.IsMiniMapActive = false;
            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);
        }
    }
}
