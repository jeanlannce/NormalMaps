using System;
using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Airdrop;
using EFT.SynchronizableObjects;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    // 移植自 v1.2.1：空投箱落地检测（AirdropMarkerProvider 依赖）
    internal class AirdropBoxOnBoxLandPatch : ModulePatch
    {
        internal static List<AirdropSynchronizableObject> Airdrops = new List<AirdropSynchronizableObject>();

        private static bool _hasRegisteredEvents;

        internal static event Action<AirdropSynchronizableObject> OnAirdropLanded;

        protected override MethodBase GetTargetMethod()
        {
            if (!_hasRegisteredEvents)
            {
                GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
                _hasRegisteredEvents = true;
            }

            return AccessTools.Method(typeof(ClientAirDrop), nameof(ClientAirDrop.CloseParachute));
        }

        [PatchPostfix]
        public static void PatchPostfix(ClientAirDrop __instance)
        {
            try
            {
                if (__instance != null && __instance._syncObject != null && !Airdrops.Contains(__instance._syncObject))
                {
                    Airdrops.Add(__instance._syncObject);
                    OnAirdropLanded?.Invoke(__instance._syncObject);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Caught error in AirdropBoxOnBoxLandPatch: {e.Message}");
            }
        }

        internal static void OnRaidEnd()
        {
            Airdrops.Clear();
        }
    }
}
