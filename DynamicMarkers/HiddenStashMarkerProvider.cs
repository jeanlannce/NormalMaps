using System.Collections.Generic;
using System.Linq;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT.Interactive;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    // 移植自 v1.2.1：战局内显示隐藏仓库标记（GameStartedPatch 收集容器，受情报等级限制）
    public class HiddenStashMarkerProvider : IDynamicMarkerProvider
    {
        private const string HiddenCacheCategory = "Hidden Stash";
        private const string HiddenCacheImagePath = "Markers/barrel.png";
        private const string HiddenCacheName = "Hidden Stash";

        private MapView _lastMapView;
        private Dictionary<LootableContainer, MapMarker> _stashMarkers = new Dictionary<LootableContainer, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            foreach (var hiddenStash in GameStartedPatch.HiddenStashes)
            {
                TryAddMarker(hiddenStash);
            }
        }

        public void OnHideInRaid(MapView map)
        {
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;
            foreach (var item in _stashMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            foreach (var item in _stashMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }

        private void TryAddMarker(LootableContainer stash)
        {
            if (_stashMarkers.ContainsKey(stash))
            {
                return;
            }

            // 情报等级门槛：未达标不显示（0 = 总是显示）
            if (Settings.ShowHiddenStashIntelLevel.Value > (GameUtils.GetIntelLevel() ?? 0))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = HiddenCacheCategory,
                Color = Settings.HiddenStashColor.Value,
                ImagePath = HiddenCacheImagePath,
                Position = MathUtils.ConvertToMapPosition(stash.transform),
                Text = HiddenCacheName,
            };
            _stashMarkers[stash] = _lastMapView.AddMapMarker(markerDef);
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _stashMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void TryRemoveMarker(LootableContainer stash)
        {
            if (_stashMarkers.ContainsKey(stash))
            {
                _stashMarkers[stash].ContainingMapView.RemoveMapMarker(_stashMarkers[stash]);
                _stashMarkers.Remove(stash);
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
        }

        public void OnHideOutOfRaid(MapView map)
        {
        }
    }
}
