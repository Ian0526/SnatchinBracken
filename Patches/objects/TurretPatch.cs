using GameNetcodeStuff;
using HarmonyLib;
using SnatchinBracken.Patches.data;

namespace SnatchinBracken.Patches
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("CheckForPlayersInLineOfSight")]
        static void PostfixCheckForPlayersInLineOfSight(Turret __instance, ref PlayerControllerB __result, float radius, bool angleRangeCheck)
        {
            if (SharedData.Instance.IgnoreTurrets && __result != null && SharedData.Instance.BindedDrags.ContainsValue(__result))
            {
                __result = null;
            }
        }
    }

}
