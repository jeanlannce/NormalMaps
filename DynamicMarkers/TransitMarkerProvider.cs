using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    // 移植自 v1.2.1：战局内显示转运点标记
    public class TransitMarkerProvider : IDynamicMarkerProvider
    {
        private const string TransitCategory = "Transit";
        private const string TransitImagePath = "Markers/transit.png";

        private Dictionary<TransitPoint, MapMarker> _transitMarkers = new Dictionary<TransitPoint, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            if (_transitMarkers.Count == 0)
            {
                AddTransitMarkers(map);
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
            foreach (var item in _transitMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(map, item);
            }
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        public void RefreshMarkers(MapView map)
        {
            foreach (var item in _transitMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(map, item);
            }
        }

        private void AddTransitMarkers(MapView map)
        {
            var transitController = Singleton<GameWorld>.Instance.TransitController;
            if (transitController == null)
            {
                return;
            }

            foreach (var point in transitController.pointsById.Values)
            {
                TryAddMarker(map, point);
            }
        }

        private void TryAddMarker(MapView map, TransitPoint point)
        {
            if (_transitMarkers.ContainsKey(point))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = TransitCategory,
                ImagePath = TransitImagePath,
                Text = point.parameters.description.BSGLocalized(),
                Position = MathUtils.ConvertToMapPosition(point.transform),
                Color = Settings.TransPointColor.Value,
            };
            _transitMarkers[point] = map.AddMapMarker(markerDef);
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _transitMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void TryRemoveMarker(TransitPoint transit)
        {
            if (_transitMarkers.ContainsKey(transit))
            {
                _transitMarkers[transit].ContainingMapView.RemoveMapMarker(_transitMarkers[transit]);
                _transitMarkers.Remove(transit);
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
