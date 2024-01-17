using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(FlowermanAI))]
    internal class BrackenAIPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.FlowermanAI";

        private static ManualLogSource mls;

        static BrackenAIPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch("KillPlayerAnimationServerRpc")]
        static bool PrefixKillPlayerAnimationServerRpc(FlowermanAI __instance, int playerObjectId)
        {
            if (!__instance.IsHost && !__instance.IsServer) return true;
            if (__instance == null)
            {
                mls.LogError("FlowermanAI instance is null.");
                return true;
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObjectId];
            if (player == null)
            {
                return true;
            }
            mls.LogInfo("Bracken identified playerObjectId " + playerObjectId);

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

            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: true);
            __instance.carryingPlayerBody = true;
            player.inSpecialInteractAnimation = true;
            __instance.inKillAnimation = false;
            __instance.targetPlayer = null;

            player.GetComponent<FlowermanBinding>().BindPlayerServerRpc(playerObjectId, __instance.NetworkObjectId);

            Transform transform = __instance.ChooseFarthestNodeFromPosition(RoundManager.FindMainEntrancePosition());
            if (__instance.favoriteSpot == null)
            {
                __instance.favoriteSpot = transform;
            }

            __instance.SwitchToBehaviourStateOnLocalClient(1);
            if (__instance.IsServer)
            {
                __instance.SwitchToBehaviourState(1);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("HitEnemy")]
        static void HitEnemyPatch(FlowermanAI __instance, int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false)
        {
            if (!__instance.IsHost && !__instance.IsServer)
            {
                mls.LogInfo("Not host, not running");
                return;
            }
            if (SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                mls.LogInfo("Contained");
                PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
                int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);
                ManuallyDropPlayerOnHit(__instance, player);
                player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, __instance.NetworkObjectId);
            }
            mls.LogInfo("Finished hit");
        }

        private static void ManuallyDropPlayerOnHit(FlowermanAI __instance, PlayerControllerB player)
        {
            player.inSpecialInteractAnimation = false;
            __instance.carryingPlayerBody = false;
            __instance.creatureAnimator.SetBool("killing", value: false);
            __instance.creatureAnimator.SetBool("carryingBody", value: false);

            __instance.FinishKillAnimation(false);
        }

        [HarmonyPrefix]
        [HarmonyPatch("DropPlayerBody")]
        static bool DropBodyPatch(FlowermanAI __instance)
        {
            if (!__instance.IsHost && !__instance.IsServer) return true;
            if (__instance == null)
            {
                mls.LogInfo("FlowermanAI instance is null in DropBodyPatch.");
                return true;
            }

            if (!SharedData.Instance.BindedDrags.ContainsKey(__instance))
            {
                mls.LogInfo("FlowermanAI instance not found in BindedDrags map.");
                return true;
            }

            PlayerControllerB player = SharedData.Instance.BindedDrags.GetValueSafe(__instance);
            int id = SharedData.Instance.PlayerIDs.GetValueSafe(player);

            if (player == null)
            {
                mls.LogError("PlayerControllerB instance is null in BindedDrags map.");
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
