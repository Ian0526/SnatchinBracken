using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using SnatchingBracken.Patches.network;
using SnatchingBracken.Utils;
using UnityEngine;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class TeleporterPatch
    {
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
                FlowermanAI flowerman = GeneralUtils.SearchForCorrelatedFlowerman(__instance);
                if (flowerman != null)
                {
                    if (SharedData.Instance.AllowTeleports)
                    {

                        int id = SharedData.Instance.PlayerIDs[__instance];
                        SharedData.UpdateTimestampNow(flowerman, __instance);
                        GeneralUtils.ManuallyUnbindPlayer(flowerman, __instance);

                        FlowermanBinding flowermanBinding = __instance.gameObject.GetComponent<FlowermanBinding>();
                        if (flowermanBinding != null)
                        {
                            __instance.gameObject.GetComponent<FlowermanBinding>().ResetEntityStatesServerRpc(id, flowerman.NetworkObjectId);
                            __instance.gameObject.GetComponent<FlowermanBinding>().UnbindPlayerServerRpc(id, flowerman.NetworkObjectId);
                            __instance.gameObject.GetComponent<FlowermanBinding>().UnmufflePlayerVoiceServerRpc(id);
                            __instance.gameObject.GetComponent<FlowermanBinding>().GiveChillPillServerRpc(id);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
