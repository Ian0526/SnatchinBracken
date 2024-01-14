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
        static bool PrefixTriggerEntry(Turret __instance, Collider other)
        {
            if (!SharedData.Instance.IgnoreMines) { return true; }

            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (!(component != GameNetworkManager.Instance.localPlayerController)
                && component != null && SharedData.Instance.BindedDrags.ContainsValue(component)
                && !component.isPlayerDead)
            {
                mls.LogInfo("Player would've triggered Mine here, preventing.");
                return false;
            }
            return true;
        }
    }

}
