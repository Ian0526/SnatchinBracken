using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
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

            mls.LogInfo("Current time elapsed: " + (Time.time - lastGrabbed));
            mls.LogInfo("Max time is " + SharedData.Instance.KillAtTime);
            if (Time.time - lastGrabbed >= (SharedData.Instance.KillAtTime))
            {
                UnbindPlayerAndBracken(player, flowermanAI);
                FinishKillAnimationNormally(flowermanAI, player, id);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("PlayerIsTargetable")]
        static bool PlayerIsTargetablePatch(EnemyAI __instance, PlayerControllerB playerScript, bool cannotBeInShip = false)
        {
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
            SharedData.Instance.LastGrabbedTimeStamp.Remove(__instance);
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
