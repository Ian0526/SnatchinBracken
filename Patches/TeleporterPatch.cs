using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using UnityEngine;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class TeleporterPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Teleporter";

        private static ManualLogSource mls;

        static TeleporterPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch("TeleportPlayer")]
        static bool PrefixTeleportPlayer(PlayerControllerB __instance, Vector3 pos, bool withRotation = false, float rot = 0f, bool allowInteractTrigger = false, bool enableController = true)
        {
            if (!__instance.IsHost && !__instance.IsServer) return true;
            if (__instance == null)
            {
                return true;
            }

            if (SharedData.Instance.BindedDrags.ContainsValue(__instance))
            {
                FlowermanAI flowerman = SearchForCorrelatedFlowerman(__instance);
                if (flowerman != null)
                {
                    ManuallyUnbindPlayer(flowerman, __instance);
                }
            }
            return true;
        }

        private static FlowermanAI SearchForCorrelatedFlowerman(PlayerControllerB player)
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

        private static void ManuallyUnbindPlayer(FlowermanAI flowerman, PlayerControllerB player)
        {
            int playerId = SharedData.Instance.PlayerIDs.GetValueSafe(player);
            player.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(playerId, flowerman.NetworkObjectId);

            player.inSpecialInteractAnimation = false;

            flowerman.carryingPlayerBody = false;
            flowerman.creatureAnimator.SetBool("killing", value: false);
            flowerman.creatureAnimator.SetBool("carryingBody", value: false);
            flowerman.inSpecialAnimation = false;
            flowerman.inKillAnimation = false;
            flowerman.FinishKillAnimation(false);
            flowerman.SwitchToBehaviourState(2);
            flowerman.CancelKillAnimationClientRpc((int) playerId);
        }
    }

}
