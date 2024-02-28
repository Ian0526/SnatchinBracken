using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using SnatchingBracken.Patches.tasks;
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

        // Unbinds the player from the Bracken if they are teleported during the dragging
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
                    if (SharedData.Instance.AllowTeleports)
                    {
                        int id = SharedData.Instance.PlayerIDs[__instance];
                        SharedData.UpdateTimestampNow(flowerman, __instance);
                        ManuallyUnbindPlayer(flowerman, __instance);
                        flowerman.HitEnemy(0);
                        __instance.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(id, flowerman.NetworkObjectId);
                        __instance.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, flowerman.NetworkObjectId);
                        __instance.gameObject.GetComponent<FlowermanBinding>().UnmufflePlayerVoiceServerRpc(id);
                        __instance.gameObject.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(id);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Finds the correlated Bracken by comparing the IDs of the dictionary values, ideally I should make
        // another dictionary inversing the key and values for optimization
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

        // Unbinds the player to the Bracken, after the kill is called, patches in the BrackenAIPatch.cs file
        // will handle RPC methods properly
        private static void ManuallyUnbindPlayer(FlowermanAI flowerman, PlayerControllerB player)
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
    }
}
