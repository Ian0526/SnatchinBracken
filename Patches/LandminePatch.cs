using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;
using UnityEngine;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Landmine";

        private static ManualLogSource mls;

        static LandminePatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnTriggerEnter")]
        static bool PrefixTriggerEntry(Landmine __instance, Collider other)
        {
            if (!SharedData.Instance.IgnoreMines) { return true; }

            // Check if the collider is a Bracken and is carrying a body
            FlowermanAI flowermanAI = other.gameObject.GetComponentInParent<FlowermanAI>();
            if (flowermanAI != null && SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
            {
                mls.LogInfo("Bracken carrying a body triggered Mine, preventing.");
                return false;
            }

            // Check if the collider is a player being dragged
            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (component != null && SharedData.Instance.BindedDrags.ContainsValue(component) && !component.isPlayerDead)
            {
                mls.LogInfo("Player being dragged would've triggered Mine here, preventing.");
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnTriggerExit")]
        static bool PostfixTriggerExit(Landmine __instance, Collider other)
        {
            if (!SharedData.Instance.IgnoreMines) { return true; }

            // Check if the collider is a Bracken and is carrying a body
            FlowermanAI flowermanAI = other.gameObject.GetComponentInParent<FlowermanAI>();
            if (flowermanAI != null && SharedData.Instance.BindedDrags.ContainsKey(flowermanAI))
            {
                mls.LogInfo("Bracken carrying a body triggered Mine, preventing.");
                return false;
            }

            // Check if the collider is a player being dragged
            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (component != null && SharedData.Instance.BindedDrags.ContainsValue(component) && !component.isPlayerDead)
            {
                mls.LogInfo("Player being dragged would've triggered Mine here, preventing.");
                return false;
            }
            return true;
        }
    }
}
