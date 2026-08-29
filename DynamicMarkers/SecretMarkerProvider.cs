using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    // 移植自 v1.2.1：战局内显示秘密撤离点标记（含状态着色）
    public class SecretMarkerProvider : IDynamicMarkerProvider
    {
        private const string SecretCategory = "Secret";
        private const string SecretImagePath = "Markers/exit.png";

        // 与 0.3.4 ExtractMarkerProvider 保持一致的状态颜色
        private static readonly Color ExtractOpenColor = Color.green;
        private static readonly Color ExtractHasRequirementsColor = Color.yellow;

        private bool _showExtractStatusInRaid = true;
        private Dictionary<SecretExfiltrationPoint, MapMarker> _secretMarkers = new Dictionary<SecretExfiltrationPoint, MapMarker>();

        public bool ShowExtractStatusInRaid
        {
            get => _showExtractStatusInRaid;
            set
            {
                if (_showExtractStatusInRaid == value)
                {
                    return;
                }

                _showExtractStatusInRaid = value;
                foreach (var key in _secretMarkers.Keys)
                {
                    UpdateSecretExtractStatus(key, key.Status);
                }
            }
        }

        public void OnShowInRaid(MapView map)
        {
            if (_secretMarkers.Count == 0)
            {
                AddSecretMarkers(map);
            }

            foreach (var key in _secretMarkers.Keys)
            {
                UpdateSecretExtractStatus(key, key.Status);
                key.OnStatusChanged += UpdateSecretExtractStatus;
            }
        }

        public void OnHideInRaid(MapView map)
        {
            foreach (var key in _secretMarkers.Keys)
            {
                key.OnStatusChanged -= UpdateSecretExtractStatus;
            }
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            foreach (var item in _secretMarkers.Keys.ToList())
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
            foreach (var item in _secretMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(map, item);
            }
        }

        private void AddSecretMarkers(MapView map)
        {
            var instance = Singleton<GameWorld>.Instance;
            foreach (var point in instance.ExfiltrationController.SecretExfiltrationPoints)
            {
                TryAddMarker(map, point);
            }
        }

        private void TryAddMarker(MapView map, SecretExfiltrationPoint point)
        {
            if (_secretMarkers.ContainsKey(point))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = SecretCategory,
                ImagePath = SecretImagePath,
                Text = point.Settings.Name.BSGLocalized(),
                Position = MathUtils.ConvertToMapPosition(point.transform),
                Color = Settings.SecretPointColor.Value,
            };
            _secretMarkers[point] = map.AddMapMarker(markerDef);
            UpdateSecretExtractStatus(point, point.Status);
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _secretMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void UpdateSecretExtractStatus(ExfiltrationPoint point, EExfiltrationStatus status)
        {
            var secretPoint = point as SecretExfiltrationPoint;
            if (secretPoint == null || !_secretMarkers.ContainsKey(secretPoint))
            {
                return;
            }

            var mapMarker = _secretMarkers[secretPoint];
            if (!_showExtractStatusInRaid)
            {
                mapMarker.Color = Settings.SecretPointColor.Value;
                return;
            }

            // 状态颜色（与 v1.2.1 一致，数值对应 EExfiltrationStatus）：
            //   2 = 条件未满足（黄）/ 3 = 已开启（绿）/ 其他 = 默认色
            int statusValue = (int)status;
            if (statusValue == 3)
            {
                mapMarker.Color = ExtractOpenColor;
            }
            else if (statusValue == 2)
            {
                mapMarker.Color = ExtractHasRequirementsColor;
            }
            else
            {
                mapMarker.Color = Settings.SecretPointColor.Value;
            }
        }

        private void TryRemoveMarker(SecretExfiltrationPoint secret)
        {
            if (_secretMarkers.ContainsKey(secret))
            {
                secret.OnStatusChanged -= UpdateSecretExtractStatus;
                _secretMarkers[secret].ContainingMapView.RemoveMapMarker(_secretMarkers[secret]);
                _secretMarkers.Remove(secret);
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
