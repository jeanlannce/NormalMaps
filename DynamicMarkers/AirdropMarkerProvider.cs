using System.Collections.Generic;
using System.Linq;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.SynchronizableObjects;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    // 移植自 v1.2.1：战局内显示空投箱标记（落地时出现）
    public class AirdropMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;

        private Dictionary<AirdropSynchronizableObject, MapMarker> _airdropMarkers = new Dictionary<AirdropSynchronizableObject, MapMarker>();

        private const string _airdropName = "Airdrop";
        private const string _airdropCategory = "Airdrop";
        private const string _airdropImagePath = "Markers/airdrop.png";
        private static Vector2 _airdropPivot = new Vector2(0.5f, 0.25f);

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            foreach (var airdrop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(airdrop);
            }

            AirdropBoxOnBoxLandPatch.OnAirdropLanded += TryAddMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            AirdropBoxOnBoxLandPatch.OnAirdropLanded -= TryAddMarker;
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var item in _airdropMarkers.Keys.ToList())
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

            foreach (var keyValuePair in _airdropMarkers.ToArray())
            {
                TryRemoveMarker(keyValuePair.Key);
            }

            foreach (var airdrop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(airdrop);
            }
        }

        private void TryAddMarker(AirdropSynchronizableObject airdrop)
        {
            if (airdrop == null || _airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = _airdropCategory,
                Color = Settings.AirdropColor.Value,
                ImagePath = _airdropImagePath,
                Position = MathUtils.ConvertToMapPosition(airdrop.transform),
                Pivot = _airdropPivot,
                Text = _airdropName,
            };

            _airdropMarkers[airdrop] = _lastMapView.AddMapMarker(markerDef);
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void TryRemoveMarker(AirdropSynchronizableObject airdrop)
        {
            if (_airdropMarkers.ContainsKey(airdrop))
            {
                _airdropMarkers[airdrop].ContainingMapView.RemoveMapMarker(_airdropMarkers[airdrop]);
                _airdropMarkers.Remove(airdrop);
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
