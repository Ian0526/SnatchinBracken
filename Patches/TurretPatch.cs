using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Turret";

        private static ManualLogSource mls;

        static TurretPatch()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPostfix]
        [HarmonyPatch("CheckForPlayersInLineOfSight")]
        static void PostfixCheckForPlayersInLineOfSight(Turret __instance, ref PlayerControllerB __result, float radius, bool angleRangeCheck)
        {
            if (SharedData.Instance.IgnoreTurrets && __result != null && SharedData.Instance.BindedDrags.ContainsValue(__result))
            {
                mls.LogInfo("Ignoring player targeted by Turret due to being dragged.");
                __result = null; // Ignore this player
            }
        }
    }

}
