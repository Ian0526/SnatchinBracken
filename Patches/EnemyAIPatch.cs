using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using System.Linq;
using UnityEngine;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.EnemyAI";

        private static readonly ManualLogSource mls;

        private static float distnaceThreshold = 0.1f;
        private static float minForceMultiplier = 1f;
        private static float maxForceMultiplier = 10f;
        private static float maxDistance = 10f;

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

            if (player == null)
            {
                return;
            }

            if (player.isPlayerDead)
            {
                UnbindPlayerAndBracken(player, flowermanAI);
                return;
            }

            UpdatePosition(flowermanAI, player);

            if (__instance.IsHost || __instance.IsServer)
            {

                int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                float lastGrabbed = SharedData.Instance.LastGrabbedTimeStamp[flowermanAI];
                float distance = Vector3.Distance(__instance.transform.position, __instance.favoriteSpot.position);

                if (Time.time - lastGrabbed >= (SharedData.Instance.KillAtTime) || (distance <= SharedData.Instance.DistanceFromFavorite))
                {
                    SharedData.UpdateTimestampNow(flowermanAI);
                    UnbindPlayerAndBracken(player, flowermanAI);
                    FinishKillAnimationNormally(flowermanAI, player, (int)id);
                }
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
                    return false;
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("TargetClosestPlayer")]
        static bool ClosestPlayerPatch(FlowermanAI __instance)
        {
            if (!__instance.IsHost && !__instance.IsServer) return true;
            return !SharedData.Instance.BindedDrags.ContainsKey(__instance);
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
            if (!SharedData.Instance.PlayerIDs.ContainsKey(player))
            {
                mls.LogInfo("There isn't a player bound to this Bracken. That's strange.");
                return;
            }
            int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
            player.inSpecialInteractAnimation = false;
            player.inAnimationWithEnemy = null;

            __instance.carryingPlayerBody = true;
            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: false);
            __instance.stunnedByPlayer = null;
            __instance.stunNormalizedTimer = 0f;
            __instance.angerMeter = 0f;
            __instance.isInAngerMode = false;
            __instance.timesThreatened = 0;
            __instance.FinishKillAnimation(false);

            RemoveDictionaryReferences(__instance, player, id);
        }

        static void RemoveDictionaryReferences(FlowermanAI __instance, PlayerControllerB player, int playerId)
        {
            player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(playerId, __instance.NetworkObjectId);
            player.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(playerId, __instance.NetworkObjectId);
        }

        static void FinishKillAnimationNormally(FlowermanAI __instance, PlayerControllerB playerControllerB, int playerId)
        {
            __instance.inSpecialAnimationWithPlayer = playerControllerB;
            playerControllerB.inSpecialInteractAnimation = true;
            __instance.KillPlayerAnimationClientRpc(playerId);
        }
    }
}
