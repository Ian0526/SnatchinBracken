using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;

namespace SnatchingBracken.Utils
{
    internal class GeneralUtils
    {

        private const string modGUID = "Ovchinikov.SnatchinBracken.Utils";

        private static ManualLogSource mls;

        private static GeneralUtils instance;

        public static GeneralUtils Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GeneralUtils();
                    mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
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
            SharedData.Instance.LocationCoroutineStarted.Remove(__instance);
            player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(playerId, __instance.NetworkObjectId);
            player.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(playerId, __instance.NetworkObjectId);
            player.gameObject.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(playerId);
        }

        public static void UnbindPlayerAndBracken(PlayerControllerB player, FlowermanAI __instance)
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
            mls.LogInfo("Bracken found good spot to kill, killing player.");
            __instance.inSpecialAnimationWithPlayer = playerControllerB;
            playerControllerB.inSpecialInteractAnimation = true;
            __instance.KillPlayerAnimationClientRpc(playerId);
        }
    }
}
