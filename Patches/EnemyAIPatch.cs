using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchingBracken.Patches.data;
using UnityEngine;

namespace SnatchingBracken.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken";
        private static ManualLogSource mls;

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
            UpdatePosition(flowermanAI, player);
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
            float distance = Vector3.Distance(__instance.transform.position, __instance.favoriteSpot.position);
        }
    }
}
