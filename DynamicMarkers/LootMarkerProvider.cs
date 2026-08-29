using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    // 移植自 v1.2.1：战局内显示愿望清单中的物品标记（受情报等级限制）
    public class LootMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<LootItem, MapMarker> _lootMarkers = new Dictionary<LootItem, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            foreach (IKillable loot in Singleton<GameWorld>.Instance.LootList)
            {
                var lootItem = loot as LootItem;
                if (lootItem != null && GameUtils.GetWishListItems().Contains(new MongoID(lootItem.TemplateId)))
                {
                    TryAddMarker(lootItem);
                }
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
            foreach (var item in _lootMarkers.Keys.ToList())
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

            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }

        private void TryAddMarker(LootItem item)
        {
            if (item == null || item.Item == null || _lootMarkers.ContainsKey(item))
            {
                return;
            }

            // 情报等级门槛：未达标不显示（0 = 总是显示）
            if (Settings.ShowWishListItemsIntelLevel.Value > (GameUtils.GetIntelLevel() ?? 0))
            {
                return;
            }

            // 4.1.3 防御：EFTHardSettings.StaticIcons 可能为 null（新版本结构变化），拿不到物品图标时跳过 Sprite
            Sprite sprite = null;
            try
            {
                var staticIcons = EFTHardSettings.Instance?.StaticIcons;
                if (staticIcons != null)
                {
                    var itemType = ItemViewFactory.GetItemType(item.Item.GetType());
                    sprite = staticIcons.ItemTypeSprites.GetValueOrDefault(itemType);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"LootMarkerProvider: failed to get item sprite, using fallback: {e.Message}");
            }

            var transform = item.transform;
            if (transform == null)
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = "Loot",
                Color = Settings.LootItemColor.Value,
                Sprite = sprite,
                Position = MathUtils.ConvertToMapPosition(transform),
                Text = item.Item.TemplateId.BSGLocalized(),
            };
            _lootMarkers[item] = _lastMapView.AddMapMarker(markerDef);
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void TryRemoveMarker(LootItem item)
        {
            if (_lootMarkers.ContainsKey(item))
            {
                _lootMarkers[item].ContainingMapView.RemoveMapMarker(_lootMarkers[item]);
                _lootMarkers.Remove(item);
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
