using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using System.Linq;
using UnityEngine;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.EnemyAI";

        private static readonly ManualLogSource mls;

        static EnemyAIPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void FlowermanStart(EnemyAI __instance)
        {
            if (__instance is FlowermanAI flowermanAI)
            {
                SharedData.Instance.FlowermanIDs[__instance.NetworkObjectId] = flowermanAI;
                mls.LogInfo("We've binded the ID " + flowermanAI.NetworkObjectId + " to flowerman object");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        static void UpdatePatcher(EnemyAI __instance)
        {
            if (!(__instance is FlowermanAI flowermanAI) || !SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
            {
                return;
            }
            PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(flowermanAI);
            int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
            UpdatePosition(flowermanAI, player);
            float lastGrabbed = SharedData.Instance.LastGrabbedTimeStamp[flowermanAI];

            if (Time.time - lastGrabbed >= (SharedData.Instance.KillAtTime))
            {
                UnbindPlayerAndBracken(player, flowermanAI);
                FinishKillAnimationNormally(flowermanAI, player, (int) id);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("MeetsStandardPlayerCollisionConditions")]
        static bool OverrideCollisionCheck(EnemyAI __instance, Collider other, bool inKillAnimation = false, bool overrideIsInsideFactoryCheck = false)
        {
            if (!__instance.IsHost) return true;
            if (!(__instance is FlowermanAI flowerman)) return true;
            if (SharedData.Instance.LastGrabbedTimeStamp.ContainsKey(flowerman))
            {
                if (Time.time - SharedData.Instance.LastGrabbedTimeStamp[flowerman] <= SharedData.Instance.SecondsBeforeNextAttempt)
                {
                    mls.LogInfo("times " + Time.time + " " + SharedData.Instance.LastGrabbedTimeStamp[flowerman] + " <= " + SharedData.Instance.SecondsBeforeNextAttempt);
                    mls.LogInfo("Blocking collision");
                    return false;
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("PlayerIsTargetable")]
        static bool PlayerIsTargetablePatch(EnemyAI __instance, PlayerControllerB playerScript, bool cannotBeInShip = false)
        {
            if (__instance is FlowermanAI flowermanAI)
            {
                if (SharedData.Instance.LastGrabbedTimeStamp.ContainsKey(flowermanAI))
                {
                    if (Time.time - SharedData.Instance.LastGrabbedTimeStamp[flowermanAI] <= SharedData.Instance.SecondsBeforeNextAttempt)
                    {
                        return false;
                    }
                }
                return !SharedData.Instance.BindedDrags.ContainsKey(flowermanAI);

            }
            return !SharedData.Instance.BindedDrags.ContainsValue(playerScript);
        }

        static void UpdatePosition(FlowermanAI __instance, PlayerControllerB player)
        {
            player.transform.position = __instance.transform.position;
        }

        static void UnbindPlayerAndBracken(PlayerControllerB player, FlowermanAI __instance)
        {
            player.inSpecialInteractAnimation = false;
            __instance.carryingPlayerBody = false;
            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: false);
            RemoveDictionaryReferences(__instance);
        }

        static void RemoveDictionaryReferences(FlowermanAI __instance)
        {
            SharedData.Instance.BindedDrags.Remove(__instance);
            SharedData.Instance.LastGrabbedTimeStamp[__instance] = Time.time;
        }

        static void FinishKillAnimationNormally(FlowermanAI __instance, PlayerControllerB playerControllerB, int playerId)
        {
            mls.LogInfo("Bracken found good spot to kill, killing player.");
            __instance.inSpecialAnimationWithPlayer = playerControllerB;
            playerControllerB.inSpecialInteractAnimation = true;
            __instance.KillPlayerAnimationClientRpc(playerId);
        }
    }
}
