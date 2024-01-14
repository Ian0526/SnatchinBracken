using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
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
                if (entry.Value == player)
                {
                    return entry.Key;
                }
            }
            return null;
        }

        private static void ManuallyUnbindPlayer(FlowermanAI flowerman, PlayerControllerB player)
        {
            player.inSpecialInteractAnimation = false;

            flowerman.carryingPlayerBody = false;
            flowerman.creatureAnimator.SetBool("killing", value: false);
            flowerman.creatureAnimator.SetBool("carryingBody", value: false);

            SharedData.Instance.BindedDrags.Remove(flowerman);
            SharedData.Instance.LastGrabbedTimeStamp.Remove(flowerman);
        }
    }

}
