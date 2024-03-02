using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using SnatchingBracken.Patches.tasks;
using SnatchingBracken.Utils;
using System.Collections;
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

        // Just initializes the Bracken's network object id
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void FlowermanStart(EnemyAI __instance)
        {
            if (__instance is FlowermanAI flowermanAI)
            {
                SharedData.Instance.FlowermanIDs[__instance.NetworkObjectId] = flowermanAI;
            }
        }

        // This is the ticker method, handles movement and other general updates about the Bracken
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
                GeneralUtils.UnbindPlayerAndBracken(player, flowermanAI);
                return;
            }

            UpdatePosition(flowermanAI, player);

            if (__instance.IsHost || __instance.IsServer)
            {

                int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                float lastGrabbed = SharedData.Instance.LastGrabbedTimeStamp[flowermanAI];
                float distance = Vector3.Distance(__instance.transform.position, __instance.favoriteSpot.position);

                if ((Time.time - lastGrabbed >= (SharedData.Instance.KillAtTime) || (distance <= SharedData.Instance.DistanceFromFavorite)) && !SharedData.Instance.DoDamageOnInterval)
                {
                    SharedData.UpdateTimestampNow(flowermanAI, player);
                    GeneralUtils.UnbindPlayerAndBracken(player, flowermanAI);
                    player.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(id);
                    FlowermanLocationTask task = __instance.gameObject.GetComponent<FlowermanLocationTask>();
                    if (task != null)
                    {
                        task.StopCheckStuckCoroutine();
                    }
                    GeneralUtils.FinishKillAnimationNormally(flowermanAI, player, (int)id);
                }
            }
        }

        // Player's that are bound will indefinitely collide with the Bracken, this prevents it
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

        // Prevents the Bracken from targeting other players when they've bound with one
        [HarmonyPrefix]
        [HarmonyPatch("TargetClosestPlayer")]
        static bool ClosestPlayerPatch(FlowermanAI __instance)
        {
            if (!__instance.IsHost && !__instance.IsServer) return true;
            return !SharedData.Instance.BindedDrags.ContainsKey(__instance);
        }

        // Prevents other mobs from targeting the player while being dragged.
        [HarmonyPrefix]
        [HarmonyPatch("PlayerIsTargetable")]
        static bool PlayerIsTargetablePatch(EnemyAI __instance, PlayerControllerB playerScript, bool cannotBeInShip = false)
        {
            if (SharedData.Instance.MonstersIgnorePlayers)
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
            return true;
        }

        // Sets the player's position to the Bracken's with a minor deviation in the direction the Bracken is holding the player
        static void UpdatePosition(FlowermanAI __instance, PlayerControllerB player)
        {
            float distanceInFront = -0.8f;
            Vector3 newPosition = __instance.transform.position + __instance.transform.forward * distanceInFront;
            player.transform.position = newPosition;
        }
    }
}