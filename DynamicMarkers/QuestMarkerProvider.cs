using System;
using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;

namespace DynamicMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private List<MapMarker> _questMarkers = new List<MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            if (GameUtils.IsScavRaid())
            {
                return;
            }

            AddQuestObjectiveMarkers(map);
        }

        public void OnHideInRaid(MapView map)
        {
            // TODO: don't just be lazy and try to update markers
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            TryRemoveMarkers();
            AddQuestObjectiveMarkers(map);
        }

        public void OnRaidEnd(MapView map)
        {
            QuestUtils.DiscardQuestData();
            TryRemoveMarkers();
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            try
            {
                QuestUtils.TryCaptureQuestData();

                var player = GameUtils.GetMainPlayer();
                if (player == null)
                {
                    return;
                }

                var markerDefs = QuestUtils.GetMarkerDefsForPlayer(player);
                if (markerDefs == null)
                {
                    return;
                }

                foreach (var markerDef in markerDefs)
                {
                    var marker = map.AddMapMarker(markerDef);
                    _questMarkers.Add(marker);
                }
            }
            catch (Exception e)
            {
                // 4.1.3 Quest API 反射适配尚未完成，任务标记暂缺（不影响其他功能）
                Plugin.Log.LogWarning($"QuestMarkerProvider: quest markers unavailable: {e.Message}");
            }
        }

        private void TryRemoveMarkers()
        {
            foreach (var marker in _questMarkers)
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
            }
            _questMarkers.Clear();
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
