using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchingBracken.Patches.data;
using UnityEngine;

namespace SnatchingBracken.Patches
{
    [HarmonyPatch(typeof(FlowermanAI))]
    internal class BrackenAIPatch
    {
        private const string modGUID = "Ovchinikov.SnatchingBracken";

        private static ManualLogSource mls;

        static BrackenAIPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch("KillPlayerAnimationServerRpc")]
        static bool PrefixKillPlayerAnimationServerRpc(FlowermanAI __instance, int playerObjectId)
        {
            if (__instance == null)
            {
                mls.LogError("FlowermanAI instance is null.");
                return true;
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObjectId];
            if (player == null)
            {
                mls.LogError("PlayerControllerB instance is null.");
                return true; 
            }

            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: true);
            __instance.carryingPlayerBody = true;
            player.inSpecialInteractAnimation = true;
            __instance.inKillAnimation = false;
            __instance.targetPlayer = null;

            SharedData.Instance.BindedDrags[__instance] = player;
            SharedData.Instance.PlayerIDs[player] = playerObjectId;

            Transform transform = __instance.ChooseFarthestNodeFromPosition(RoundManager.FindMainEntrancePosition());
            if (__instance.favoriteSpot == null)
            {
                __instance.favoriteSpot = transform;
            }
            __instance.SwitchToBehaviourState(1);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("HitEnemy")]
        static void HitEnemyPatch(FlowermanAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            ManuallyDropPlayerOnHit(__instance, playerWhoHit);
        }

        private static void ManuallyDropPlayerOnHit(FlowermanAI __instance, PlayerControllerB player)
        {
            player.inSpecialInteractAnimation = false;
            __instance.carryingPlayerBody = false;
            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: false);
        }

        [HarmonyPrefix]
        [HarmonyPatch("DropPlayerBody")]
        static bool DropBodyPatch(FlowermanAI __instance)
        {
            if (__instance == null)
            {
                mls.LogError("FlowermanAI instance is null in DropBodyPatch.");
                return true;
            }

            if (!SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                mls.LogError("FlowermanAI instance not found in BindedDrags map.");
                return true;
            }

            PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
            int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);

            if (player == null)
            {
                mls.LogError("PlayerControllerB instance is null in BindedDrags map.");
                SharedData.Instance.BindedDrags.Remove(__instance); // Clean up the map
                return true;
            }

            mls.LogInfo("Found controller, releasing player from special interaction.");
            player.inSpecialInteractAnimation = false;
            SharedData.Instance.BindedDrags.Remove(__instance);
            __instance.carryingPlayerBody = false;
            __instance.bodyBeingCarried = null;
            __instance.creatureAnimator.SetBool("carryingBody", value: false);
            FinishKillAnimationNormally(__instance, player, id);
            return false;
        }

        static void ImmediatelyChangeStatesAfterKill(FlowermanAI __instance)
        {
            __instance.evadeStealthTimer = 0f;
            __instance.SwitchToBehaviourState(0);
            __instance.carryingPlayerBody = false;
            __instance.bodyBeingCarried = null;
        }

        static bool IsBrackenDoneDragging(FlowermanAI __instance)
        {
            if (__instance.favoriteSpot != null && Vector3.Distance(__instance.transform.position, __instance.favoriteSpot.transform.position) < 5f)
            {
                return true;
            }
            return false;
        }

        static void FinishKillAnimationNormally(FlowermanAI __instance, PlayerControllerB playerControllerB, int playerId)
        {
            mls.LogInfo("Killing player.");
            __instance.inSpecialAnimationWithPlayer = playerControllerB;
            playerControllerB.inSpecialInteractAnimation = true;
            __instance.KillPlayerAnimationClientRpc(playerId);
        }
    }
}
