using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;
using System.Collections.Generic;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(FlowermanAI))]
    internal class BrackenAIPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.FlowermanAI";

        private static ManualLogSource mls;

        private static List<FlowermanAI> JustProcessed = new List<FlowermanAI>();

        static BrackenAIPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch("KillPlayerAnimationServerRpc")]
        static bool PrefixKillPlayerAnimationServerRpc(FlowermanAI __instance, int playerObjectId)
        {
            if (!__instance.IsHost && !__instance.IsServer)
            {
                return true;
            }

            if (__instance == null)
            {
                return true;
            }

            if (CountAlivePlayers() <= 1 && SharedData.Instance.InstantKillIfAlone)
            {
                return true;
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObjectId];

            if (player == null)
            {
                return true;
            }

            if (SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                return false;
            }

            if (SharedData.Instance.BindedDrags.ContainsValue(player))
            {
                return false;
            }

            if (SharedData.Instance.LastGrabbedTimeStamp.ContainsKey(__instance))
            {
                if (Time.time - SharedData.Instance.LastGrabbedTimeStamp[__instance] <= SharedData.Instance.SecondsBeforeNextAttempt)
                {
                    return false;
                }
            }

            if (SharedData.Instance.DropItems)
            {
                player.DropAllHeldItemsAndSync();
            }
            else
            {
                DropDoubleHandedItem(player);
            }

            Rigidbody[] componentsInChildren = player.gameObject.GetComponentsInChildren<Rigidbody>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].isKinematic = false;
                mls.LogInfo("The component's name is:" + componentsInChildren[i].name);
            }

            player.GetComponent<FlowermanBinding>().PrepForBindingServerRpc(playerObjectId, __instance.NetworkObjectId);
            player.GetComponent<FlowermanBinding>().BindPlayerServerRpc(playerObjectId, __instance.NetworkObjectId);
            player.GetComponent<FlowermanBinding>().UpdateFavoriteSpotServerRpc(playerObjectId, __instance.NetworkObjectId);

            __instance.SwitchToBehaviourStateOnLocalClient(1);
            if (__instance.IsServer)
            {
                __instance.SwitchToBehaviourState(1);
            }
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("HitEnemy")]
        static void HitEnemyPostPatch(FlowermanAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            if (!__instance.IsHost && !__instance.IsServer)
            {
                if (JustProcessed.Contains(__instance))
                {
                    __instance.angerMeter = 0;
                    __instance.isInAngerMode = false;
                    __instance.angerCheckInterval = 0;

                    JustProcessed.Remove(__instance);
                }
                return;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("HitEnemy")]
        static void HitEnemyPrePatch(FlowermanAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            if (!__instance.IsHost && !__instance.IsServer)
            {
                return;
            }
            if (SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
                int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                SharedData.UpdateTimestampNow(__instance);
                ManuallyDropPlayerOnHit(__instance, player);

                player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
                player.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(id, __instance.NetworkObjectId);
                JustProcessed.Add(__instance);
            }
        }


        private static int CountAlivePlayers()
        {
            return StartOfRound.Instance.livingPlayers;
        }

        private static void ManuallyDropPlayerOnHit(FlowermanAI __instance, PlayerControllerB player)
        {
            player.inSpecialInteractAnimation = false;
            player.inAnimationWithEnemy = null;

            __instance.carryingPlayerBody = false;
            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: false);
            __instance.angerMeter = 0f;
            __instance.isInAngerMode = false;
            __instance.stunnedByPlayer = null;
            __instance.stunNormalizedTimer = 0f;
            __instance.evadeStealthTimer = 0.1f;
            __instance.timesThreatened = 0;

            __instance.FinishKillAnimation(false);
        }

        [HarmonyPrefix]
        [HarmonyPatch("DropPlayerBody")]
        static bool DropBodyPatch(FlowermanAI __instance)
        {
            if (!__instance.IsHost && !__instance.IsServer) return true;

            if (!SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                return true;
            }

            if (!SharedData.Instance.ChaoticTendencies && !PrerequisiteKilling(__instance))
            {
                return false;
            }

            PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
            int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);

            if (player == null)
            {
                SharedData.Instance.BindedDrags.Remove(__instance);
                return true;
            }

            player.inSpecialInteractAnimation = false;
            player.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);

            __instance.carryingPlayerBody = false;
            __instance.bodyBeingCarried = null;
            __instance.creatureAnimator.SetBool("carryingBody", value: false);

            FinishKillAnimationNormally(__instance, player, (int) id);

            return false;
        }

        static bool PrerequisiteKilling(FlowermanAI flowerman)
        {
            if (SharedData.Instance.LastGrabbedTimeStamp.ContainsKey(flowerman))
            {

                float lastGrabbed = SharedData.Instance.LastGrabbedTimeStamp[flowerman];
                float distance = Vector3.Distance(flowerman.transform.position, flowerman.favoriteSpot.position);

                if (Time.time - lastGrabbed >= (SharedData.Instance.KillAtTime) || (distance <= SharedData.Instance.DistanceFromFavorite))
                {
                    return true;
                }
            }
            return false;
        }

        static void FinishKillAnimationNormally(FlowermanAI __instance, PlayerControllerB playerControllerB, int playerId)
        {
            mls.LogInfo("Bracken found good spot to kill, killing player.");
            __instance.inSpecialAnimationWithPlayer = playerControllerB;
            playerControllerB.inSpecialInteractAnimation = true;
            __instance.KillPlayerAnimationClientRpc(playerId);
        }

        static void DropDoubleHandedItem(PlayerControllerB player, bool itemsFall = true, bool disconnecting = false)
        {
            if (player.twoHanded)
            {
                player.DiscardHeldObject();
            }
        }
    }
}
