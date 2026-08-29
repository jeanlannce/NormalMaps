using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.UI.Controls;
using DynamicMaps.Utils;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private const string _mapRelPath = "Maps";

        private static float _positionTweenTime = 0.25f;
        private static float _scrollZoomScaler = 1.75f;
        private static float _zoomScrollTweenTime = 0.25f;

        private static Vector2 _levelSliderPosition = new Vector2(15f, 750f);
        private static Vector2 _mapSelectDropdownPosition = new Vector2(-780f, -50f);
        private static Vector2 _mapSelectDropdownSize = new Vector2(360f, 31f);
        private static Vector2 _maskSizeModifierInRaid = new Vector2(0, -42f);
        private static Vector2 _maskPositionInRaid = new Vector2(0, -20f);
        private static Vector2 _maskSizeModifierOutOfRaid = new Vector2(0, -70f);
        private static Vector2 _maskPositionOutOfRaid = new Vector2(0, -5f);
        private static Vector2 _textAnchor = new Vector2(0f, 1f);
        private static Vector2 _cursorPositionTextOffset = new Vector2(15f, -52f);
        private static Vector2 _playerPositionTextOffset = new Vector2(15f, -68f);
        private static float _positionTextFontSize = 15f;

        public bool IsReplacingMapScreen = true;
        public bool IsShowingMapScreen { get; private set; }
        public RectTransform RectTransform => gameObject.GetRectTransform();
        public MapView MapView => _mapView;

        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private bool _isShown = false;

        // map and transport mechanism
        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private MapView _mapView;

        // map controls
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;
        private CursorPositionText _cursorPositionText;
        private PlayerPositionText _playerPositionText;

        // peek
        private MapPeekComponent _peekComponent;
        private bool _isPeeking => _peekComponent != null && _peekComponent.IsPeeking;

        // v1.2.1 移植：小地图（MiniMap）
        private bool ShowingMiniMap => _peekComponent != null && _peekComponent.ShowingMiniMap;
        private EventHandler _adjustMiniMapHandler;
        private static readonly float _miniMapUpdateInterval = 0.033f;
        private float _miniMapUpdateTimer;
        private Vector2 _savedMainMapPos = Vector2.zero;
        private bool _rememberMapPosition;
        private KeyboardShortcut _zoomMiniMapInShortcut;
        private KeyboardShortcut _zoomMiniMapOutShortcut;
        private bool _zoomMainMapToMouse;

        // dynamic map marker providers
        private Dictionary<Type, IDynamicMarkerProvider> _dynamicMarkerProviders = new Dictionary<Type, IDynamicMarkerProvider>();

        // config
        private bool _autoCenterOnPlayerMarker = true;
        private bool _autoSelectLevel = true;
        private bool _resetZoomOnCenter = false;
        private float _centeringZoomResetPoint = 0f;
        private KeyboardShortcut _centerPlayerShortcut;
        private KeyboardShortcut _dumpShortcut;
        private KeyboardShortcut _moveMapUpShortcut;
        private KeyboardShortcut _moveMapDownShortcut;
        private KeyboardShortcut _moveMapLeftShortcut;
        private KeyboardShortcut _moveMapRightShortcut;
        private float _moveMapSpeed = 0.25f;
        private bool _moveMapYInverted = false;
        private KeyboardShortcut _moveMapLevelUpShortcut;
        private KeyboardShortcut _moveMapLevelDownShortcut;
        private KeyboardShortcut _zoomMapInShortcut;
        private KeyboardShortcut _zoomMapOutShortcut;
        private float _zoomMapHotkeySpeed = 2.5f;
        private float _peekZoomScale = 1.0f;

        internal static ModdedMapScreen Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");
            return go.AddComponent<ModdedMapScreen>();
        }

        private void Awake()
        {
            // make our game object hierarchy
            var scrollRectGO = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGO = UIUtils.CreateUIGameObject(scrollRectGO, "ScrollMask");

            // v1.2.1 移植：小地图位置/尺寸配置变更时即时调整
            _adjustMiniMapHandler = (s, e) => AdjustForMiniMap(false);
            Settings.MiniMapPosition.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapScreenOffsetX.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapScreenOffsetY.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapSizeX.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapSizeY.SettingChanged += _adjustMiniMapHandler;

            _mapView = MapView.Create(scrollMaskGO, "MapView");

            // set up mask; size will be set later in Raid/NoRaid
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();

            // set up scroll rect
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapView.RectTransform;

            // create map controls

            // level select slider
            var sliderPrefab = Singleton<CommonUI>.Instance.transform.Find(
                "Common UI/InventoryScreen/Map Panel/MapBlock/ZoomScroll").gameObject;
            _levelSelectSlider = LevelSelectSlider.Create(sliderPrefab, RectTransform);
            _levelSelectSlider.OnLevelSelectedBySlider += _mapView.SelectTopLevel;
            _mapView.OnLevelSelected += (level) => _levelSelectSlider.SelectedLevel = level;

            // map select dropdown, this will call LoadMap on the first option
            var selectPrefab = Singleton<CommonUI>.Instance.transform.Find(
                "Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            _mapSelectDropdown = MapSelectDropdown.Create(selectPrefab, RectTransform);
            _mapSelectDropdown.OnMapSelected += ChangeMap;

            // texts
            _cursorPositionText = CursorPositionText.Create(gameObject, _mapView.RectTransform, _positionTextFontSize);
            _cursorPositionText.RectTransform.anchorMin = _textAnchor;
            _cursorPositionText.RectTransform.anchorMax = _textAnchor;

            _playerPositionText = PlayerPositionText.Create(gameObject, _positionTextFontSize);
            _playerPositionText.RectTransform.anchorMin = _textAnchor;
            _playerPositionText.RectTransform.anchorMax = _textAnchor;
            _playerPositionText.gameObject.SetActive(false);

            // read config before setting up marker providers
            ReadConfig();

            GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;

            // load initial maps from path
            // 注意：不做启动预缓存——SvgUtils 渲染大 SVG 是同步阻塞（单个 0.5~3 秒），
            // 启动/进战局时逐帧渲染 36 张会卡死在“正在加载地图”。改为按需渲染 + 缓存（切图秒开）。
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);
        }

        private void OnDestroy()
        {
            if (_adjustMiniMapHandler != null)
            {
                Settings.MiniMapPosition.SettingChanged -= _adjustMiniMapHandler;
                Settings.MiniMapScreenOffsetX.SettingChanged -= _adjustMiniMapHandler;
                Settings.MiniMapScreenOffsetY.SettingChanged -= _adjustMiniMapHandler;
                Settings.MiniMapSizeX.SettingChanged -= _adjustMiniMapHandler;
                Settings.MiniMapSizeY.SettingChanged -= _adjustMiniMapHandler;
            }

            GameWorldOnDestroyPatch.OnRaidEnd -= OnRaidEnd;
        }

        private void Update()
        {
            // because we have a scroll rect, it seems to eat OnScroll via IScrollHandler
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                if (!_mapSelectDropdown.isActiveAndEnabled || !_mapSelectDropdown.IsDropdownOpen())
                {
                    OnScroll(scroll);
                }
            }

            // change level hotkeys（小地图模式下不响应图层切换）
            if (!ShowingMiniMap && _moveMapLevelUpShortcut.BetterIsDown())
            {
                _levelSelectSlider.ChangeLevelBy(1);
            }

            if (!ShowingMiniMap && _moveMapLevelDownShortcut.BetterIsDown())
            {
                _levelSelectSlider.ChangeLevelBy(-1);
            }

            // shift hotkeys（小地图模式下方向键不移动大地图，避免与小地图缩放热键冲突）
            var shiftMapX = 0f;
            var shiftMapY = 0f;
            if (!ShowingMiniMap)
            {
                var ySign = _moveMapYInverted ? -1f : 1f;
                if (_moveMapUpShortcut.BetterIsPressed())
                {
                    shiftMapY += 1f * ySign;
                }

                if (_moveMapDownShortcut.BetterIsPressed())
                {
                    shiftMapY -= 1f * ySign;
                }

                if (_moveMapLeftShortcut.BetterIsPressed())
                {
                    shiftMapX -= 1f;
                }

                if (_moveMapRightShortcut.BetterIsPressed())
                {
                    shiftMapX += 1f;
                }
            }

            if (shiftMapX != 0f || shiftMapY != 0f)
            {
                _mapView.ScaledShiftMap(new Vector2(shiftMapX, shiftMapY), _moveMapSpeed * Time.deltaTime, false);
            }

            // zoom hotkeys（v1.2.1 移植：小地图模式用独立缩放热键，大地图模式用主缩放热键）
            if (ShowingMiniMap)
            {
                OnZoomMini();
            }
            else
            {
                OnZoomMain();
            }

            OnCenter();

            if (_dumpShortcut.BetterIsDown())
            {
                DumpUtils.DumpExtracts();
                DumpUtils.DumpSwitches();
                DumpUtils.DumpLocks();
            }
        }

        // private void OnDisable()
        // {
        //     OnHide();
        // }

        internal void OnMapScreenShow()
        {
            if (_peekComponent != null)
            {
                _peekComponent.WasMiniMapActive = ShowingMiniMap;
                _peekComponent.EndPeek();
                _peekComponent.EndMiniMap();
            }

            IsShowingMapScreen = true;

            // 4.1.3 兼容：MapBlock/EmptyBlock 可能不存在（UI 结构变化），null 时跳过隐藏
            transform.parent.Find("MapBlock")?.gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock")?.gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);

            Show(false);
        }

        internal void OnMapScreenClose()
        {
            Hide();

            IsShowingMapScreen = false;

            // v1.2.1 移植：关闭地图界面后恢复小地图
            if (_peekComponent != null && _peekComponent.WasMiniMapActive)
            {
                _mapView.ApplyMiniMapZoom();
                _peekComponent.BeginMiniMap();
            }
        }

        internal void Show(bool playAnimation = true)
        {
            AdjustSizeAndPosition();

            _isShown = true;
            gameObject.SetActive(GameUtils.ShouldShowMapInRaid());

            // populate map select dropdown
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);

            if (GameUtils.IsInRaid())
            {
                Plugin.Log.LogInfo("Showing map in raid");
                OnShowInRaid(playAnimation);
            }
            else
            {
                Plugin.Log.LogInfo("Showing map out-of-raid");
                OnShowOutOfRaid();
            }
        }

        internal void Hide()
        {
            _mapSelectDropdown?.TryCloseDropdown();

            // close isn't called when hidden
            if (GameUtils.IsInRaid())
            {
                Plugin.Log.LogInfo("Hiding map in raid");
                OnHideInRaid();
            }
            else
            {
                Plugin.Log.LogInfo("Hiding map out-of-raid");
                OnHideOutOfRaid();
            }

            _isShown = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// NormalMaps 增强：peek 全景观测——缩到最小缩放并将地图中心对准画面中心
        /// </summary>
        internal void FullMapPanorama()
        {
            if (_mapView == null)
            {
                return;
            }

            try
            {
                // 全景缩放：完整地图刚好填满屏幕 × 用户可调倍率
                // （不用 ZoomMin——那是按小地图 275px mask 算的最小缩放，peek 时地图会太小）
                var screenSize = RectTransform.sizeDelta;
                var mapSize = _mapView.RectTransform.sizeDelta;
                if (screenSize.x > 0f && screenSize.y > 0f && mapSize.x > 0f && mapSize.y > 0f)
                {
                    var fitZoom = Mathf.Min(screenSize.x / mapSize.x, screenSize.y / mapSize.y);
                    _mapView.SetMapZoom(fitZoom * _peekZoomScale, 0f);
                }
                else
                {
                    _mapView.SetMapZoom(_mapView.ZoomMin, 0f);
                }

                var mapDef = _mapView.CurrentMapDef;
                if (mapDef != null)
                {
                    var midpoint = MathUtils.GetMidpoint(mapDef.Bounds.Min, mapDef.Bounds.Max);
                    _mapView.ShiftMapToCoordinate(midpoint, 0f, false);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"FullMapPanorama failed: {e.Message}");
            }
        }

        /// <summary>
        /// NormalMaps 增强：peek 结束后恢复默认状态（缩放恢复 + 跟随玩家）
        /// </summary>
        internal void RestoreDefaultState()
        {
            if (_mapView == null)
            {
                return;
            }

            try
            {
                if (_resetZoomOnCenter)
                {
                    _mapView.SetMapZoom(GetInRaidStartingZoom(), 0f);
                }

                if (_autoCenterOnPlayerMarker)
                {
                    var player = GameUtils.GetMainPlayer();
                    if (player != null)
                    {
                        var mapPosition = MathUtils.ConvertToMapPosition(player.Position);
                        _mapView.ShiftMapToCoordinate(mapPosition, 0f, false);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"RestoreDefaultState failed: {e.Message}");
            }
        }

        internal void TryAddPeekComponent(EftBattleUIScreen battleUI)
        {
            if (_peekComponent != null)
            {
                return;
            }

            // 藏身处（HideoutGame）也会触发 BattleUIScreen.Show——不挂 peek/小地图组件
            if (GameUtils.IsInHideout())
            {
                Plugin.Log.LogInfo("Skipping peek component: in hideout");
                return;
            }

            Plugin.Log.LogInfo("Trying to attach peek component to BattleUI");

            _peekComponent = MapPeekComponent.Create(battleUI.gameObject);
            _peekComponent.MapScreen = this;
            _peekComponent.MapScreenTrueParent = _parentTransform;

            ReadConfig();
        }

        internal void OnRaidEnd()
        {
            _savedMainMapPos = Vector2.zero;
            _mapView.IsMiniMapActive = false;

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnRaidEnd(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnRaidEnd");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }

            // reset peek and remove reference, it will be destroyed very shortly with parent object
            _peekComponent?.EndPeek();
            _peekComponent?.EndMiniMap();
            Destroy(_peekComponent.gameObject);
            _peekComponent = null;

            // unload map completely when raid ends, since we've removed markers
            _mapView.UnloadMap();
        }

        private void AdjustSizeAndPosition()
        {
            // set width and height based on inventory screen
            var rect = Singleton<CommonUI>.Instance.InventoryScreen.GetRectTransform().rect;
            RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
            RectTransform.anchoredPosition = Vector2.zero;

            _scrollRect.GetRectTransform().sizeDelta = RectTransform.sizeDelta;

            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

            _levelSelectSlider.RectTransform.anchoredPosition = _levelSliderPosition;

            _mapSelectDropdown.RectTransform.sizeDelta = _mapSelectDropdownSize;
            _mapSelectDropdown.RectTransform.anchoredPosition = _mapSelectDropdownPosition;

            _cursorPositionText.RectTransform.anchoredPosition = _cursorPositionTextOffset;
            _playerPositionText.RectTransform.anchoredPosition = _playerPositionTextOffset;
        }

        private void AdjustForOutOfRaid()
        {
            ResetMaskAnchors();

            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

            // turn on cursor and off player position texts
            _cursorPositionText.gameObject.SetActive(true);
            _playerPositionText.gameObject.SetActive(false);
            _levelSelectSlider.gameObject.SetActive(true);
        }

        private void AdjustForInRaid()
        {
            ResetMaskAnchors();

            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionInRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierInRaid;

            // turn both cursor and player position texts on
            _cursorPositionText.gameObject.SetActive(true);
            _playerPositionText.gameObject.SetActive(true);

            // v1.2.1 移植：小地图模式下隐藏图层滑块
            _levelSelectSlider.gameObject.SetActive(!ShowingMiniMap);
        }

        // —— v1.2.1 移植：小地图位置/尺寸调整 ——
        private void AdjustForMiniMap(bool playAnimation)
        {
            var anchor = ConvertEnumToScreenPos(Settings.MiniMapPosition.Value);
            var offset = new Vector2(Settings.MiniMapScreenOffsetX.Value, Settings.MiniMapScreenOffsetY.Value);
            offset *= ConvertEnumToScenePivot(Settings.MiniMapPosition.Value);
            var size = new Vector2(Settings.MiniMapSizeX.Value, Settings.MiniMapSizeY.Value);

            var maskRect = _scrollMask.GetRectTransform();
            maskRect.sizeDelta = size;
            maskRect.anchoredPosition = offset;
            maskRect.anchorMin = anchor;
            maskRect.anchorMax = anchor;
            maskRect.pivot = anchor;

            // 小地图模式下隐藏文本与图层滑块
            _cursorPositionText.gameObject.SetActive(false);
            _playerPositionText.gameObject.SetActive(false);
            _levelSelectSlider.gameObject.SetActive(false);
        }

        private Vector2 ConvertEnumToScreenPos(EMiniMapPosition pos)
        {
            switch (pos)
            {
                case EMiniMapPosition.TopRight: return new Vector2(1f, 1f);
                case EMiniMapPosition.BottomRight: return new Vector2(1f, 0f);
                case EMiniMapPosition.TopLeft: return new Vector2(0f, 1f);
                case EMiniMapPosition.BottomLeft: return new Vector2(0f, 0f);
                default: return Vector2.zero;
            }
        }

        private Vector2 ConvertEnumToScenePivot(EMiniMapPosition pos)
        {
            switch (pos)
            {
                case EMiniMapPosition.TopRight: return new Vector2(-1f, -1f);
                case EMiniMapPosition.BottomRight: return new Vector2(-1f, 1f);
                case EMiniMapPosition.TopLeft: return new Vector2(1f, -1f);
                case EMiniMapPosition.BottomLeft: return new Vector2(1f, 1f);
                default: return Vector2.zero;
            }
        }

        private void AdjustForPeek()
        {
            ResetMaskAnchors();

            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = Vector2.zero;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta;

            // turn both cursor and player position texts off
            _cursorPositionText.gameObject.SetActive(false);
            _playerPositionText.gameObject.SetActive(false);
            _levelSelectSlider.gameObject.SetActive(false);
        }

        /// <summary>将 mask 的 anchor/pivot 复位为默认（小地图模式修改过它们）</summary>
        private void ResetMaskAnchors()
        {
            var maskRect = _scrollMask.GetRectTransform();
            maskRect.anchorMin = new Vector2(0.5f, 0.5f);
            maskRect.anchorMax = new Vector2(0.5f, 0.5f);
            maskRect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void OnShowInRaid(bool playAnimation)
        {
            if (ShowingMiniMap)
            {
                AdjustForMiniMap(playAnimation);
            }
            else if (_isPeeking)
            {
                AdjustForPeek();
            }
            else
            {
                AdjustForInRaid();
            }

            // filter dropdown to only maps containing the internal map name
            var mapInternalName = GameUtils.GetCurrentMapInternalName();
            _mapSelectDropdown.FilterByInternalMapName(mapInternalName);
            _mapSelectDropdown.LoadFirstAvailableMap();

            if (ShowingMiniMap)
            {
                _levelSelectSlider.gameObject.SetActive(false);
            }

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnShowInRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnShowInRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }

            // rest of this function needs player
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(player.Position);

            // select layers to show
            if (_autoSelectLevel)
            {
                _mapView.SelectLevelByCoords(mapPosition);
            }

            // v1.2.1 移植：小地图模式下不重置主地图视角（大地图位置/缩放独立记忆）
            if (ShowingMiniMap)
            {
                return;
            }

            if (_rememberMapPosition && _mapView.MainMapPos != Vector2.zero)
            {
                _mapView.ApplyMainMapZoom();
                _mapView.SetMapPos(_savedMainMapPos, 0f);
            }
            else if (!_rememberMapPosition && !_autoCenterOnPlayerMarker)
            {
                _mapView.ApplyMainMapZoom();
                _mapView.SetMapZoom(_mapView.ZoomMin, 0f);
            }
            else if (_autoCenterOnPlayerMarker)
            {
                _mapView.ApplyMainMapZoom();

                // change zoom to desired level
                if (_resetZoomOnCenter)
                {
                    _mapView.SetMapZoom(GetInRaidStartingZoom(), 0);
                }

                // shift map to player position, Vector3 to Vector2 discards z
                _mapView.ShiftMapToPlayer(mapPosition, 0, false);
            }
        }

        private void OnHideInRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnHideInRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnHideInRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnShowOutOfRaid()
        {
            AdjustForOutOfRaid();

            // clear filter on dropdown
            _mapSelectDropdown.ClearFilter();

            // load first available map if no maps loaded
            if (_mapView.CurrentMapDef == null)
            {
                _mapSelectDropdown.LoadFirstAvailableMap();
            }

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnShowOutOfRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnShowOutOfRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnHideOutOfRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnHideOutOfRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnHideOutOfRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        // —— v1.2.1 移植：主地图缩放（仅主键盘不参与；小键盘 8/5 与配置热键一致）——
        private void OnZoomMain()
        {
            var zoomAmount = 0f;
            if (_zoomMapOutShortcut.BetterIsPressed())
            {
                zoomAmount -= 1f;
            }

            if (_zoomMapInShortcut.BetterIsPressed())
            {
                zoomAmount += 1f;
            }

            if (zoomAmount != 0f)
            {
                var zoomDelta = _mapView.ZoomMain * zoomAmount * (_zoomMapHotkeySpeed * Time.deltaTime);

                if (_isPeeking)
                {
                    var player = GameUtils.GetMainPlayer();
                    var mapPosition = player != null ? MathUtils.ConvertToMapPosition(player.Position) : Vector3.zero;
                    _mapView.IncrementalZoomInto(zoomDelta, mapPosition, 0f);
                }
                else if (_zoomMainMapToMouse)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _mapView.RectTransform, Input.mousePosition, null, out Vector2 rectPoint);
                    _mapView.IncrementalZoomInto(zoomDelta, rectPoint, 0f);
                }
                else
                {
                    var rectPoint = _mapView.RectTransform.anchoredPosition / _mapView.ZoomMain;
                    _mapView.IncrementalZoomInto(zoomDelta, rectPoint, 0f);
                }
            }
            else
            {
                _mapView.SetMapZoom(_mapView.ZoomMain, 0f);
            }
        }

        // —— v1.2.1 移植：小地图缩放（小键盘 8/5）——
        private void OnZoomMini()
        {
            var zoomAmount = 0f;
            if (_zoomMiniMapOutShortcut.BetterIsPressed())
            {
                zoomAmount -= 1f;
            }

            if (_zoomMiniMapInShortcut.BetterIsPressed())
            {
                zoomAmount += 1f;
            }

            if (zoomAmount != 0f)
            {
                var player = GameUtils.GetMainPlayer();
                var mapPosition = player != null ? MathUtils.ConvertToMapPosition(player.Position) : Vector3.zero;
                var zoomDelta = _mapView.ZoomMini * zoomAmount * (_zoomMapHotkeySpeed * Time.deltaTime);
                _mapView.IncrementalZoomIntoMiniMap(zoomDelta, mapPosition, 0f);
            }
            else
            {
                _mapView.SetMapZoom(_mapView.ZoomMini, 0f, false, true);
            }
        }

        // —— v1.2.1 移植：居中/跟随玩家 ——
        private void OnCenter()
        {
            if (_centerPlayerShortcut.BetterIsDown())
            {
                CenterOnPlayer(false);
            }
            else if (ShowingMiniMap)
            {
                // 小地图定时跟随玩家（约 30Hz）
                _miniMapUpdateTimer -= Time.deltaTime;
                if (_miniMapUpdateTimer <= 0f)
                {
                    _miniMapUpdateTimer = _miniMapUpdateInterval;
                    CenterOnPlayer(true);
                }
            }
            else if (_autoCenterOnPlayerMarker && GameUtils.IsInRaid() && !_isPeeking && _mapView != null)
            {
                // NormalMaps 增强：大地图每帧跟随玩家（peek 全景时保持固定，不跟随）
                var player = GameUtils.GetMainPlayer();
                if (player != null)
                {
                    var mapPosition = MathUtils.ConvertToMapPosition(player.Position);
                    _mapView.ShiftMapToCoordinate(mapPosition, 0f, false);
                }
            }
        }

        private void CenterOnPlayer(bool isMini)
        {
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(player.Position);
            _mapView.ShiftMapToCoordinate(mapPosition, isMini ? 0f : _positionTweenTime, isMini);
            _mapView.SelectLevelByCoords(mapPosition);
        }

        internal void SaveMainMapPos()
        {
            if (_rememberMapPosition)
            {
                _savedMainMapPos = _mapView.MainMapPos;
            }
        }

        private void OnScroll(float scrollAmount)
        {
            if (_isPeeking || ShowingMiniMap)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (scrollAmount > 0)
                {
                    _levelSelectSlider.ChangeLevelBy(1);
                }
                else
                {
                    _levelSelectSlider.ChangeLevelBy(-1);
                }

                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            var zoomDelta = scrollAmount * _mapView.ZoomCurrent * _scrollZoomScaler;
            _mapView.IncrementalZoomInto(zoomDelta, mouseRelative, _zoomScrollTweenTime);
        }

        internal void ReadConfig()
        {
            IsReplacingMapScreen = Settings.ReplaceMapScreen.Value;
            _centerPlayerShortcut = Settings.CenterOnPlayerHotkey.Value;
            _dumpShortcut = Settings.DumpInfoHotkey.Value;

            _moveMapUpShortcut = Settings.MoveMapUpHotkey.Value;
            _moveMapDownShortcut = Settings.MoveMapDownHotkey.Value;
            _moveMapLeftShortcut = Settings.MoveMapLeftHotkey.Value;
            _moveMapRightShortcut = Settings.MoveMapRightHotkey.Value;
            _moveMapSpeed = Settings.MapMoveHotkeySpeed.Value;
            _moveMapYInverted = Settings.MoveMapYInverted.Value;

            _moveMapLevelUpShortcut = Settings.ChangeMapLevelUpHotkey.Value;
            _moveMapLevelDownShortcut = Settings.ChangeMapLevelDownHotkey.Value;

            _zoomMapInShortcut = Settings.ZoomMapInHotkey.Value;
            _zoomMapOutShortcut = Settings.ZoomMapOutHotkey.Value;
            _zoomMapHotkeySpeed = Settings.ZoomMapHotkeySpeed.Value;
            _peekZoomScale = Settings.PeekZoomScale.Value;

            // v1.2.1 移植：小地图配置
            _zoomMiniMapInShortcut = Settings.ZoomInMiniMapHotkey.Value;
            _zoomMiniMapOutShortcut = Settings.ZoomOutMiniMapHotkey.Value;
            _zoomMainMapToMouse = true;  // 保持 0.3.4 行为：缩放跟随鼠标
            _rememberMapPosition = false;  // 0.3.4 无该配置，默认不记忆位置

            _autoCenterOnPlayerMarker = Settings.AutoCenterOnPlayerMarker.Value;
            _autoSelectLevel = Settings.AutoSelectLevel.Value;

            _resetZoomOnCenter = Settings.ResetZoomOnCenter.Value;
            _centeringZoomResetPoint = Settings.CenteringZoomResetPoint.Value;

            if (_peekComponent != null)
            {
                _peekComponent.PeekShortcut = Settings.PeekShortcut.Value;
                _peekComponent.HoldForPeek = Settings.HoldForPeek.Value;
                _peekComponent.HideMinimapShortcut = Settings.MiniMapShowOrHide.Value;
            }

            AddRemoveMarkerProvider<PlayerMarkerProvider>(Settings.ShowPlayerMarker.Value);
            AddRemoveMarkerProvider<QuestMarkerProvider>(Settings.ShowQuestsInRaid.Value);
            AddRemoveMarkerProvider<LockedDoorMarkerMutator>(Settings.ShowLockedDoorStatus.Value);
            AddRemoveMarkerProvider<BackpackMarkerProvider>(Settings.ShowDroppedBackpackInRaid.Value);
            AddRemoveMarkerProvider<BTRMarkerProvider>(Settings.ShowBTRInRaid.Value);
            AddRemoveMarkerProvider<AirdropMarkerProvider>(Settings.ShowAirdropsInRaid.Value);

            // v1.2.1 移植标记提供器：愿望清单 / 隐藏仓库 / 转运点 / 秘密撤离点 / 直升机坠毁
            AddRemoveMarkerProvider<LootMarkerProvider>(Settings.ShowWishListItemsInRaid.Value);
            AddRemoveMarkerProvider<HiddenStashMarkerProvider>(Settings.ShowHiddenStashesInRaid.Value);
            AddRemoveMarkerProvider<TransitMarkerProvider>(Settings.ShowTransitPointsInRaid.Value);
            AddRemoveMarkerProvider<SecretMarkerProvider>(Settings.ShowSecretPointsInRaid.Value);
            AddRemoveMarkerProvider<HeliCrashMarkerProvider>(Settings.ShowHeliCrashMarker.Value);

            if (Settings.ShowAirdropsInRaid.Value)
            {
                GetMarkerProvider<AirdropMarkerProvider>()?.RefreshMarkers();
            }

            // extracts
            AddRemoveMarkerProvider<ExtractMarkerProvider>(Settings.ShowExtractsInRaid.Value);
            if (Settings.ShowExtractsInRaid.Value)
            {
                var provider = GetMarkerProvider<ExtractMarkerProvider>();
                provider.ShowExtractStatusInRaid = Settings.ShowExtractStatusInRaid.Value;
            }

            // other player markers
            var needOtherPlayerMarkers = Settings.ShowFriendlyPlayerMarkersInRaid.Value
                                      || Settings.ShowEnemyPlayerMarkersInRaid.Value
                                      || Settings.ShowBossMarkersInRaid.Value
                                      || Settings.ShowScavMarkersInRaid.Value;

            AddRemoveMarkerProvider<OtherPlayersMarkerProvider>(needOtherPlayerMarkers);
            if (needOtherPlayerMarkers)
            {
                var provider = GetMarkerProvider<OtherPlayersMarkerProvider>();
                provider.ShowFriendlyPlayers = Settings.ShowFriendlyPlayerMarkersInRaid.Value;
                provider.ShowEnemyPlayers = Settings.ShowEnemyPlayerMarkersInRaid.Value;
                provider.ShowScavs = Settings.ShowScavMarkersInRaid.Value;
                provider.ShowBosses = Settings.ShowBossMarkersInRaid.Value;
            }

            // corpse markers
            var needCorpseMarkers = Settings.ShowFriendlyCorpsesInRaid.Value
                                 || Settings.ShowKilledCorpsesInRaid.Value
                                 || Settings.ShowFriendlyKilledCorpsesInRaid.Value
                                 || Settings.ShowBossCorpsesInRaid.Value
                                 || Settings.ShowOtherCorpsesInRaid.Value;

            AddRemoveMarkerProvider<CorpseMarkerProvider>(needCorpseMarkers);
            if (needCorpseMarkers)
            {
                var provider = GetMarkerProvider<CorpseMarkerProvider>();
                provider.ShowFriendlyCorpses = Settings.ShowFriendlyCorpsesInRaid.Value;
                provider.ShowKilledCorpses = Settings.ShowKilledCorpsesInRaid.Value;
                provider.ShowFriendlyKilledCorpses = Settings.ShowFriendlyKilledCorpsesInRaid.Value;
                provider.ShowBossCorpses = Settings.ShowBossCorpsesInRaid.Value;
                provider.ShowOtherCorpses = Settings.ShowOtherCorpsesInRaid.Value;
            }
        }

        private void AddRemoveMarkerProvider<T>(bool status) where T : IDynamicMarkerProvider, new()
        {
            if (status && !_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)] = new T();

                // if the map is shown, need to call OnShowXXXX (mapView may be null when config changes before map init)
                if (_mapView != null && _isShown && GameUtils.IsInRaid())
                {
                    _dynamicMarkerProviders[typeof(T)].OnShowInRaid(_mapView);
                }
                else if (_mapView != null && _isShown && !GameUtils.IsInRaid())
                {
                    _dynamicMarkerProviders[typeof(T)].OnShowOutOfRaid(_mapView);
                }
            }
            else if (!status && _dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                if (_mapView != null)
                {
                    _dynamicMarkerProviders[typeof(T)].OnDisable(_mapView);
                }
                _dynamicMarkerProviders.Remove(typeof(T));
            }
        }

        private T GetMarkerProvider<T>() where T : IDynamicMarkerProvider
        {
            if (!_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                return default;
            }

            return (T)_dynamicMarkerProviders[typeof(T)];
        }

        private float GetInRaidStartingZoom()
        {
            var startingZoom = _mapView.ZoomMin;
            startingZoom += _centeringZoomResetPoint * (_mapView.ZoomMax - _mapView.ZoomMin);

            return startingZoom;
        }

        private void ChangeMap(MapDef mapDef)
        {
            if (mapDef == null || _mapView.CurrentMapDef == mapDef)
            {
                return;
            }

            Plugin.Log.LogInfo($"MapScreen: Loading map {mapDef.DisplayName}");

            _mapView.LoadMap(mapDef);

            _mapSelectDropdown.OnLoadMap(mapDef);
            _levelSelectSlider.OnLoadMap(mapDef, _mapView.SelectedLevel);

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnMapChanged(_mapView, mapDef);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in ChangeMap");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }
    }
}
