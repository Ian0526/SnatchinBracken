using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using SnatchingBracken.Patches.tasks;
using System.Runtime.CompilerServices;
using System;

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), "ChooseFarthestNodeFromPosition")]
        static void FarthestNodeAdjustment(EnemyAI __instance, ref Transform __result, Vector3 pos, bool avoidLineOfSight = false, int offset = 0, bool log = false)
        {
            if (__instance is FlowermanAI flowermanAI)
            {
                if (SharedData.Instance.BrackenRoomPosition && __result != null && SharedData.Instance.BrackenRoom)
                {
                    if (__instance.SetDestinationToPosition(SharedData.Instance.BrackenRoomPosition.position, true))
                    {
                        __result = SharedData.Instance.BrackenRoomPosition;
                        return;
                    }
                    if (__instance.SetDestinationToPosition(__result.position, checkForPath: true))
                    {
                        return;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void PostfixStart(FlowermanAI __instance)
        {
            if (__instance.IsHost || __instance.IsServer)
            {
                __instance.gameObject.AddComponent<FlowermanLocationTask>();
            }

            // run check for all
            if (SharedData.Instance.BrackenRoomPosition != null && SharedData.Instance.BrackenRoom)
            {
                __instance.favoriteSpot = SharedData.Instance.BrackenRoomPosition;
            }
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

            if ((CountAlivePlayers() <= 1 && SharedData.Instance.InstantKillIfAlone) || (RollForChance(SharedData.Instance.PercentChanceForInsta)))
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

            player.GetComponent<FlowermanBinding>().PrepForBindingServerRpc(playerObjectId, __instance.NetworkObjectId);
            player.GetComponent<FlowermanBinding>().BindPlayerServerRpc(playerObjectId, __instance.NetworkObjectId);
            player.GetComponent<FlowermanBinding>().UpdateFavoriteSpotServerRpc(playerObjectId, __instance.NetworkObjectId);

            FlowermanLocationTask task = __instance.gameObject.GetComponent<FlowermanLocationTask>();
            if (task != null && !SharedData.Instance.DoDamageOnInterval)
            {
                task.StartCheckStuckCoroutine(__instance, player);
            }

            if (!SharedData.Instance.CoroutineStarted.ContainsKey(__instance) && SharedData.Instance.DoDamageOnInterval)
            {
                __instance.StartCoroutine(DoGradualDamage(__instance, player, 1.0f, SharedData.Instance.DamageDealtAtInterval));
                SharedData.Instance.CoroutineStarted[__instance] = true;
            }

            __instance.SwitchToBehaviourStateOnLocalClient(1);
            if (__instance.IsServer)
            {
                __instance.SwitchToBehaviourState(1);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyAI), "SetEnemyStunned")]
        static void SetEnemyStunnedPrefix(EnemyAI __instance, bool setToStunned, float setToStunTime = 1f, PlayerControllerB setStunnedByPlayer = null)
        {
            if (__instance is FlowermanAI flowermanAI)
            {
                if (SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
                {
                    PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(flowermanAI);
                    StopGradualDamageCoroutine(flowermanAI, player);
                }

                if (!flowermanAI.IsHost && !flowermanAI.IsServer)
                {
                    return;
                }

                if (SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
                {
                    mls.LogInfo("Stunned bracken, dropping");
                    PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(flowermanAI);
                    int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                    SharedData.UpdateTimestampNow(flowermanAI, player);
                    FlowermanLocationTask task = flowermanAI.gameObject.GetComponent<FlowermanLocationTask>();
                    if (task != null)
                    {
                        task.StopCheckStuckCoroutine();
                    }
                    ManuallyDropPlayerOnHit(flowermanAI, player);

                    player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
                    player.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(id, __instance.NetworkObjectId);
                    JustProcessed.Add(flowermanAI);
                }
            }
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
            if (SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
                StopGradualDamageCoroutine(__instance, player);
            }

            if (!__instance.IsHost && !__instance.IsServer)
            {
                return;
            }

            if (SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                mls.LogInfo("Hit bracken, dropping");
                PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
                int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                SharedData.UpdateTimestampNow(__instance, player);
                FlowermanLocationTask task = __instance.gameObject.GetComponent<FlowermanLocationTask>();
                if (task != null)
                {
                    task.StopCheckStuckCoroutine();
                }
                ManuallyDropPlayerOnHit(__instance, player);

                player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
                player.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(id, __instance.NetworkObjectId);
                JustProcessed.Add(__instance);
            }
        }

        static IEnumerator DoGradualDamage(FlowermanAI flowermanAI, PlayerControllerB player, float damageInterval, int damageAmount)
        {
            while (!player.isPlayerDead && flowermanAI != null && SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
            {
                yield return new WaitForSeconds(damageInterval);

                if (!player.isPlayerDead && flowermanAI != null && SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
                {
                    if (player.health - damageAmount <= 0)
                    {
                        StopGradualDamageCoroutine(flowermanAI, player);
                        int id = SharedData.Instance.PlayerIDs[player];
                        SharedData.UpdateTimestampNow(flowermanAI, player);
                        FinishKillAnimationNormally(flowermanAI, player, id);
                    }
                    else
                    {
                        int id = SharedData.Instance.PlayerIDs[player];
                        player.GetComponent<FlowermanBinding>().DamagePlayerServerRpc(id, damageAmount);
                    }

                    mls.LogInfo($"Damage applied to player: {damageAmount}");
                }
                else
                {
                    StopGradualDamageCoroutine(flowermanAI, player);
                }
            }
        }

        static void StopGradualDamageCoroutine(FlowermanAI flowermanAI, PlayerControllerB player)
        {
            if (SharedData.Instance.CoroutineStarted.ContainsKey(flowermanAI))
            {
                flowermanAI.StopCoroutine(DoGradualDamage(flowermanAI, player, 1.0f, SharedData.Instance.DamageDealtAtInterval));
                SharedData.Instance.CoroutineStarted.Remove(flowermanAI);
            }
        }

        static void DoDamage(PlayerControllerB player, int damageAmount)
        {
            player.DamagePlayer(damageAmount, true, true, CauseOfDeath.Mauling);
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

            if (!SharedData.Instance.DoDamageOnInterval)
            {
                player.inSpecialInteractAnimation = false;
                player.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);

                FlowermanLocationTask task = __instance.gameObject.GetComponent<FlowermanLocationTask>();
                if (task != null)
                {
                    task.StopCheckStuckCoroutine();
                }

                __instance.carryingPlayerBody = false;
                __instance.bodyBeingCarried = null;
                __instance.creatureAnimator.SetBool("carryingBody", value: false);

                // Let the GradualDamage coroutine handle the actual death part if they want gradual
                FinishKillAnimationNormally(__instance, player, (int)id);
            }
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

        static bool RollForChance(int percentChance)
        {
            if (percentChance == 0) return false;
            if (percentChance < 0 || percentChance > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentChance), "Percent chance must be between 0 and 100.");
            }

            int roll = SharedData.RandomInstance.Next(1, 101);
            return roll <= percentChance;
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