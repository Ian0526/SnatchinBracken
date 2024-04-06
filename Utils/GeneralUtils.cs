using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using SnatchingBracken.Patches.tasks;
using System.Collections;
using UnityEngine;

namespace SnatchingBracken.Utils
{
    internal class GeneralUtils
    {

        private static GeneralUtils instance;

        public static GeneralUtils Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GeneralUtils();
                }
                return instance;
            }
        }

        // Finds the correlated Bracken by comparing the IDs of the dictionary values, ideally I should make
        // another dictionary inversing the key and values for optimization
        public static FlowermanAI SearchForCorrelatedFlowerman(PlayerControllerB player)
        {
            foreach (var entry in SharedData.Instance.BindedDrags)
            {
                if (entry.Value.actualClientId == player.actualClientId)
                {
                    return entry.Key;
                }
            }
            return null;
        }

        // without dictionary removals
        public static void ManuallyUnbindPlayer(FlowermanAI flowerman, PlayerControllerB player)
        {
            int playerId = SharedData.Instance.PlayerIDs.GetValueSafe(player);

            player.inSpecialInteractAnimation = false;

            flowerman.carryingPlayerBody = false;
            flowerman.creatureAnimator.SetBool("killing", value: false);
            flowerman.creatureAnimator.SetBool("carryingBody", value: false);
            flowerman.FinishKillAnimation(false);
            flowerman.stunnedByPlayer = null;
            flowerman.stunNormalizedTimer = 0f;
            flowerman.favoriteSpot = null;
        }

        public static void ManuallyDropPlayerOnHit(FlowermanAI __instance, PlayerControllerB player)
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

        public static void RemoveDictionaryReferences(FlowermanAI __instance, PlayerControllerB player, int playerId)
        {
            SharedData.Instance.GradualDamageCoroutineStarted.Remove(__instance);

            FlowermanBinding flowermanBinding = player.gameObject.GetComponent<FlowermanBinding>();
            flowermanBinding.ResetEntityStatesServerRpc(playerId, __instance.NetworkObjectId);
            flowermanBinding.GiveChillPillServerRpc(playerId);
            flowermanBinding.UnbindPlayerServerRpc(playerId, __instance.NetworkObjectId);
        }

        public static void StopGradualDamageCoroutine(FlowermanAI flowermanAI, PlayerControllerB player)
        {
            if (SharedData.Instance.GradualDamageCoroutineStarted.ContainsKey(flowermanAI))
            {
                flowermanAI.StopCoroutine(DoGradualDamage(flowermanAI, player, 1.0f, SharedData.Instance.DamageDealtAtInterval));
                SharedData.Instance.GradualDamageCoroutineStarted.Remove(flowermanAI);
            }
        }

        public static IEnumerator DoGradualDamage(FlowermanAI flowermanAI, PlayerControllerB player, float damageInterval, int damageAmount)
        {
            while (!player.isPlayerDead && flowermanAI != null && SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
            {
                yield return new WaitForSeconds(damageInterval);

                if (!player.isPlayerDead && flowermanAI != null && SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
                {
                    if (player.health - damageAmount <= 0)
                    {

                        StopGradualDamageCoroutine(flowermanAI, player);
                        player.inSpecialInteractAnimation = false;
                        int id = SharedData.Instance.PlayerIDs[player];

                        FlowermanBinding flowermanBinding = player.gameObject.GetComponent<FlowermanBinding>();
                        if (flowermanBinding != null)
                        {
                            flowermanBinding.UnbindPlayerServerRpc(id, flowermanAI.NetworkObjectId);
                            flowermanBinding.ResetEntityStatesServerRpc(id, flowermanAI.NetworkObjectId);
                            flowermanBinding.UnmufflePlayerVoiceServerRpc(id);
                            flowermanBinding.GiveChillPillServerRpc(id);
                        }

                        FlowermanLocationTask task = flowermanAI.gameObject.GetComponent<FlowermanLocationTask>();
                        if (task != null)
                        {
                            task.StopCheckStuckCoroutine();
                        }

                        flowermanAI.carryingPlayerBody = false;
                        flowermanAI.bodyBeingCarried = null;
                        flowermanAI.creatureAnimator.SetBool("carryingBody", value: false);

                        // Let the GradualDamage coroutine handle the actual death part if they want gradual
                        GeneralUtils.FinishKillAnimationNormally(flowermanAI, player, (int)id);
                    }
                    else
                    {
                        int id = SharedData.Instance.PlayerIDs[player];
                        player.GetComponent<FlowermanBinding>().DamagePlayerServerRpc(id, damageAmount);
                    }
                }
                else
                {
                    StopGradualDamageCoroutine(flowermanAI, player);
                }
            }
        }

        public static void UnbindPlayerAndBracken(PlayerControllerB player, FlowermanAI __instance)
        {
            if (!SharedData.Instance.PlayerIDs.ContainsKey(player))
            {
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

        // A series of checks to ensure the player is in a state that they should be killed in
        public static bool PrerequisiteKilling(FlowermanAI flowerman)
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

        // Updates the player and Bracken fields to properly initiate a kill
        public static void FinishKillAnimationNormally(FlowermanAI __instance, PlayerControllerB playerControllerB, int playerId)
        {
            __instance.inSpecialAnimationWithPlayer = playerControllerB;
            playerControllerB.inSpecialInteractAnimation = true;
            __instance.KillPlayerAnimationClientRpc(playerId);
        }
    }
}
