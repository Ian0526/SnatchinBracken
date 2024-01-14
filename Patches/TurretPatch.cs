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

        [HarmonyPrefix]
        [HarmonyPatch("CheckForPlayersInLineOfSight")]
        static bool PrefixCheckForPlayersInLineOfSight(Turret __instance, ref PlayerControllerB __result, float radius = 2f, bool angleRangeCheck = false)
        {
            if (!SharedData.Instance.IgnoreTurrets) { return true; }

            PlayerControllerB foundPlayer = __instance.CheckForPlayersInLineOfSight(radius, angleRangeCheck);
            if (foundPlayer != null && !SharedData.Instance.BindedDrags.ContainsValue(foundPlayer))
            {
                __result = foundPlayer;
                return true;
            }
            else
            {
                mls.LogInfo("Player would've been targeted by Turret, ignoring.");
                __result = null;
            }
            return false;
        }
    }

}
