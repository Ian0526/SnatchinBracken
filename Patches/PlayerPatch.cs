using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;

namespace SnatchingBracken
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Playerpatch";

        private static ManualLogSource mls;

        static PlayerPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch("IHittable.Hit")]
        static bool HitOverride(PlayerControllerB __instance, int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX = false)
        {
            if (SharedData.Instance.BindedDrags.ContainsValue(__instance)
                // if they were dropped in the last second
                || (SharedData.Instance.DroppedTimestamp.ContainsKey(__instance) && (SharedData.Instance.DroppedTimestamp[__instance] + 1f) >= Time.time))
            {
                return false;
            }
            return true;
        }
    }
}
