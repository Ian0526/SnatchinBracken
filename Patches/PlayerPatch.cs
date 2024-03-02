using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using SnatchingBracken.Utils;
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

        [HarmonyPrefix]
        [HarmonyPatch("KillPlayer")]
        static void KillPlayerPatch(PlayerControllerB __instance, Vector3 bodyVelocity, bool spawnBody = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0)
        {
            // player is bound, manually unbind
            if (SharedData.Instance.BindedDrags.ContainsValue(__instance))
            {
                FlowermanAI flowermanAI = GeneralUtils.SearchForCorrelatedFlowerman(__instance);
                if (flowermanAI != null)
                {
                    int id = SharedData.Instance.PlayerIDs[__instance];
                    __instance.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
                    __instance.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(id, __instance.NetworkObjectId);
                    __instance.gameObject.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(id);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetPlayerSanityLevel")]
        static bool SetSanityLevel(PlayerControllerB __instance)
        {
            if (SharedData.Instance.BindedDrags.ContainsValue(__instance))
            {
                return false;
            }
            return true;
        }
    }
}
