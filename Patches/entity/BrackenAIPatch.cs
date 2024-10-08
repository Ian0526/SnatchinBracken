﻿using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;
using System.Collections.Generic;
using SnatchingBracken.Patches.tasks;
using System;
using SnatchingBracken.Utils;

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

        // Prevents the Bracken AI from choosing another favorite location other than the favorite room, if configured for it
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), "ChooseFarthestNodeFromPosition")]
        static void FarthestNodeAdjustment(EnemyAI __instance, ref Transform __result, Vector3 pos, bool avoidLineOfSight = false, int offset = 0, bool doAsync = false, int maxAsyncIterations = 50, bool capDistance = false)
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

        // Adds a MonoHevaiour to Brackens on start, updates the BrackenRoom's position
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void PostfixStart(FlowermanAI __instance)
        {
            if (__instance.IsHost || __instance.IsServer)
            {
                // location management should only be done by host, we don't need
                // redundant coroutines running for everyone
                if (SharedData.Instance.StuckForceKill)
                {
                    __instance.gameObject.AddComponent<FlowermanLocationTask>();
                }
            }

            // run check for all
            if (SharedData.Instance.BrackenRoomPosition != null && SharedData.Instance.BrackenRoom)
            {
                __instance.favoriteSpot = SharedData.Instance.BrackenRoomPosition;
            }
        }

        // When the Bracken collides with a player, this prefix method is called to prevent the actual
        // animation that kills the player. This binds and adjusts the entity states for all the visual stuff
        // you see
        [HarmonyPrefix]
        [HarmonyPatch("KillPlayerAnimationServerRpc")]
        static bool PrefixKillPlayerAnimationServerRpc(FlowermanAI __instance, int playerObjectId)
        {
            mls.LogInfo("Running kill Player animation");
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
            else if (!SharedData.Instance.DropItems)
            {
                DropDoubleHandedItem(player);
            }

            FlowermanBinding flowermanBinding = player.GetComponent<FlowermanBinding>();
            flowermanBinding.PrepForBindingServerRpc(playerObjectId, __instance.NetworkObjectId);
            flowermanBinding.BindPlayerServerRpc(playerObjectId, __instance.NetworkObjectId);
            flowermanBinding.UpdateFavoriteSpotServerRpc(playerObjectId, __instance.NetworkObjectId);
            flowermanBinding.MufflePlayerVoiceServerRpc(playerObjectId);
            flowermanBinding.MakeInsaneServerRpc(playerObjectId, 49.9f);

            FlowermanLocationTask task = __instance.gameObject.GetComponent<FlowermanLocationTask>();
            if (task != null && !SharedData.Instance.DoDamageOnInterval)
            {
                task.StartCheckStuckCoroutine(__instance, player);
            }

            if (!SharedData.Instance.GradualDamageCoroutineStarted.ContainsKey(__instance) && SharedData.Instance.DoDamageOnInterval)
            {
                __instance.StartCoroutine(GeneralUtils.DoGradualDamage(__instance, player, 1.0f, SharedData.Instance.DamageDealtAtInterval));
                SharedData.Instance.GradualDamageCoroutineStarted[__instance] = true;
            }

            __instance.SwitchToBehaviourStateOnLocalClient(1);
            if (__instance.IsServer)
            {
                __instance.SwitchToBehaviourState(1);
            }
            return false;
        }

        // Handles the unbinding when a Bracken is stunned during a drag event
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyAI), "SetEnemyStunned")]
        static void SetEnemyStunnedPrefix(EnemyAI __instance, bool setToStunned, float setToStunTime = 1f, PlayerControllerB setStunnedByPlayer = null)
        {
            if (__instance is FlowermanAI flowermanAI)
            {
                if (SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
                {
                    PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(flowermanAI);
                    GeneralUtils.StopGradualDamageCoroutine(flowermanAI, player);
                }

                if (!flowermanAI.IsHost && !flowermanAI.IsServer)
                {
                    return;
                }

                if (SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
                {
                    PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(flowermanAI);
                    int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                    SharedData.UpdateTimestampNow(flowermanAI, player);
                    FlowermanLocationTask task = flowermanAI.gameObject.GetComponent<FlowermanLocationTask>();
                    if (task != null)
                    {
                        task.StopCheckStuckCoroutine();
                    }
                    GeneralUtils.ManuallyUnbindPlayer(flowermanAI, player);
                    GeneralUtils.ManuallyDropPlayerOnHit(flowermanAI, player);

                    FlowermanBinding flowermanBinding = player.GetComponent<FlowermanBinding>();
                    if (flowermanBinding != null)
                    {
                        flowermanBinding.UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
                        flowermanBinding.ResetEntityStatesServerRpc(id, __instance.NetworkObjectId);
                        flowermanBinding.UnmufflePlayerVoiceServerRpc(id);
                        flowermanBinding.GiveChillPillServerRpc(id);
                    }

                    JustProcessed.Add(flowermanAI);
                }
            }
        }

        // Post fix to adjust the Bracken's entity states after they've been hit, we still have a prefix below this method that
        // handles more stuff
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
        static bool HitEnemyPrePatch(FlowermanAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            if (SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
                GeneralUtils.StopGradualDamageCoroutine(__instance, player);
            }

            if (!__instance.IsHost && !__instance.IsServer)
            {
                return true;
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
                GeneralUtils.ManuallyUnbindPlayer(__instance, player);
                GeneralUtils.ManuallyDropPlayerOnHit(__instance, player);

                FlowermanBinding flowermanBindng = player.gameObject.GetComponent<FlowermanBinding>();
                if (flowermanBindng != null)
                {
                    flowermanBindng.UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
                    flowermanBindng.ResetEntityStatesServerRpc(id, __instance.NetworkObjectId);
                    flowermanBindng.UnmufflePlayerVoiceServerRpc(id);
                    flowermanBindng.GiveChillPillServerRpc(id);
                }

                JustProcessed.Add(__instance);
            }
            return true;
        }

        private static int CountAlivePlayers()
        {
            return StartOfRound.Instance.livingPlayers;
        }

        // Actually handles the killing part, when the Bracken drops the body, we initiate a kill
        [HarmonyPrefix]
        [HarmonyPatch("DropPlayerBody")]
        static bool DropBodyPatch(FlowermanAI __instance)
        {
            if (!__instance.carryingPlayerBody || __instance.bodyBeingCarried == null) return false;
            if (!SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                return true;
            }

            PlayerControllerB player = SharedData.Instance.BindedDrags[__instance];
            if ((!__instance.IsHost && !__instance.IsServer) || player == null) return true;

            if (!GeneralUtils.PrerequisiteKilling(__instance))
            {
                return false;
            }

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
                GeneralUtils.FinishKillAnimationNormally(__instance, player, (int)id);
            }
            return false;
        }

        // Rolls a chance for instant kill if configured for it, also could be put into a util
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

        // Drops a double handed item. If a player is holding a double handed item, their position cannot be updated
        // while in a "special animation." This is needed
        static void DropDoubleHandedItem(PlayerControllerB player, bool itemsFall = true, bool disconnecting = false)
        {
            if (player.twoHanded)
            {
                player.DiscardHeldObject();
            }
        }
    }
}