using System.Collections.Generic;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    // 移植自 v1.2.1：标记战局内的直升机坠毁点（item id: 6223349b3136504a544d1608）
    public class HeliCrashMarkerProvider : IDynamicMarkerProvider
    {
        private const string HeliItemId = "6223349b3136504a544d1608";
        private const string MarkerName = "Crashed Helicopter";
        private const string MarkerCategory = "Airdrop";
        private const string MarkerImagePath = "Markers/helicopter.png";

        private static readonly Vector2 MarkerPivot = new Vector2(0.5f, 0.25f);
        private static readonly Color MarkerColor = Color.Lerp(Color.red, Color.white, 0.333f);

        private MapView _lastMapView;
        private List<MapMarker> _heliCrashMarkers = new List<MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            if (_heliCrashMarkers.Count <= 0)
            {
                TryAddMarker();
            }
        }

        public void OnHideInRaid(MapView map)
        {
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        private void TryAddMarker()
        {
            // 防御：4.1.3 返回 Option<(Item, ItemOwnerWorldData)>，查询失败（Failed）或物品/Transform 缺失时跳过，避免异常导致标记静默失败
            var found = Singleton<GameWorld>.Instance.FindItemWithWorldData(HeliItemId);
            if (found.Failed || found.Value.item == null || found.Value.data.Transform == null)
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = MarkerCategory,
                Color = MarkerColor,
                ImagePath = MarkerImagePath,
                Position = MathUtils.ConvertToMapPosition(found.Value.data.Transform),
                Pivot = MarkerPivot,
                Text = MarkerName,
            };
            _heliCrashMarkers.Add(_lastMapView.AddMapMarker(markerDef));
        }

        private void TryRemoveMarker()
        {
            if (_heliCrashMarkers.Count == 0)
            {
                return;
            }

            _heliCrashMarkers[0].ContainingMapView.RemoveMapMarker(_heliCrashMarkers[0]);
            _heliCrashMarkers.RemoveAt(0);
        }

        public void OnShowOutOfRaid(MapView map)
        {
        }

        public void OnHideOutOfRaid(MapView map)
        {
        }
    }
}
