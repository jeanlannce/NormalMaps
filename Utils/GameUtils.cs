using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Vehicle;
using HarmonyLib;

namespace DynamicMaps.Utils
{
    public static class GameUtils
    {
        // reflection
        private static FieldInfo _playerCorpseField = AccessTools.Field(typeof(Player), "Corpse");
        private static FieldInfo _playerLastAggressorField = AccessTools.Field(typeof(Player), "LastAggressor");
        private static PropertyInfo _sessionProfileProperty = AccessTools.Property(
            ClientAppUtils.GetMainApp().GetClientBackEndSession().GetType(), "Profile");
        public static object Session => ClientAppUtils.GetMainApp().GetClientBackEndSession();
        public static Profile PlayerProfile => _sessionProfileProperty.GetValue(Session) as Profile;
        //

        private static HashSet<WildSpawnType> _trackedBosses = new HashSet<WildSpawnType>
        {
            WildSpawnType.bossBoar,             // Kaban
            WildSpawnType.bossBully,            // Reshala
            WildSpawnType.bossGluhar,           // Glukhar
            WildSpawnType.bossKilla,
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossKojaniy,          // Shturman
            WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla,
            WildSpawnType.bossZryachiy,
            WildSpawnType.gifter,               // Santa
            WildSpawnType.arenaFighterEvent,    // Blood Hounds
            WildSpawnType.sectantPriest,        // Cultist Priest
            (WildSpawnType) 4206927,            // Punisher
            (WildSpawnType) 199,                // Legion
        };

        public static bool IsInRaid()
        {
            var game = Singleton<AbstractGame>.Instance;
            var botGame = Singleton<IBotGame>.Instance;

            return ((game != null) && game.InRaid)
                || ((botGame != null) && botGame.Status != GameStatus.Stopped
                                      && botGame.Status != GameStatus.Stopping
                                      && botGame.Status != GameStatus.SoftStopping);
        }

        public static string GetCurrentMapInternalName()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer?.Location;
        }

        public static Player GetMainPlayer()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer;
        }

        /// <summary>是否允许在战局内显示地图（v1.2.1 移植；0.3.4 无“需要背包地图”机制，恒为 true）</summary>
        public static bool ShouldShowMapInRaid()
        {
            return true;
        }

        public static Profile GetPlayerProfile()
        {
            return PlayerProfile;
        }

        // —— v1.2.1 移植：愿望清单 / 情报等级（带 null 防御，4.1.3 Session 获取可能失败）——
        public static int? GetIntelLevel()
        {
            try
            {
                var profile = PlayerProfile;
                if (profile == null || profile.Hideout == null)
                {
                    return 0;
                }

                return profile.Hideout.Areas.SingleOrDefault(area => (int)area.AreaType == 11)?.Level;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"GetIntelLevel failed: {e.Message}");
                return 0;
            }
        }

        public static MongoID[] GetWishListItems()
        {
            try
            {
                var profile = PlayerProfile;
                var wishlist = profile?.WishlistManager?.GetWishlist();
                return wishlist?.Keys.ToArray() ?? Array.Empty<MongoID>();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"GetWishListItems failed: {e.Message}");
                return Array.Empty<MongoID>();
            }
        }

        public static BTRView GetBTRView()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.BtrController?.BtrView;
        }

        public static bool IsScavRaid()
        {
            var player = GetMainPlayer();
            return IsInRaid() && (player != null) && player.Side == EPlayerSide.Savage;
        }

        public static string BSGLocalized(this string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "";
            }

            // TODO: use reflection to get rid of this gclass reference
            return id.Localized();
        }

        // SPT 4.1.3: 部分本地化字段为 MongoID 类型
        public static string BSGLocalized(this MongoID id)
        {
            return BSGLocalized(id.ToString());
        }

        public static bool IsGroupedWithMainPlayer(this IPlayer player)
        {
            var mainPlayerGroupId = GetMainPlayer().GroupId;
            return !string.IsNullOrEmpty(mainPlayerGroupId) && player.GroupId == mainPlayerGroupId;
        }

        public static bool IsTrackedBoss(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage && _trackedBosses.Contains(player.Profile.Info.Settings.Role);
        }

        public static bool IsPMC(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Bear || player.Profile.Side == EPlayerSide.Usec;
        }

        public static bool DidMainPlayerKill(this IPlayer player)
        {
            var aggressor = _playerLastAggressorField.GetValue(player) as IPlayer;
            if (aggressor == null)
            {
                return false;
            }

            var mainPlayer = GetMainPlayer();
            if (aggressor.ProfileId == mainPlayer.ProfileId)
            {
                return true;
            }

            return false;
        }

        public static bool DidTeammateKill(this IPlayer player)
        {
            var aggressor = _playerLastAggressorField.GetValue(player) as IPlayer;
            if (aggressor == null || string.IsNullOrEmpty(aggressor.GroupId))
            {
                return false;
            }

            var mainPlayer = GetMainPlayer();
            if (aggressor.ProfileId != mainPlayer.ProfileId && aggressor.GroupId == GetMainPlayer().GroupId)
            {
                return true;
            }

            return false;
        }

        public static bool IsBTRShooter(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage
                && player.Profile.Info.Settings.Role == WildSpawnType.shooterBTR;
        }

        public static bool HasCorpse(this Player player)
        {
            return _playerCorpseField.GetValue(player) != null;
        }
    }
}
