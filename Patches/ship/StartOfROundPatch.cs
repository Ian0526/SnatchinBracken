using HarmonyLib;
using SnatchinBracken.Patches.data;

namespace SnatchingBracken.Patches.ship
{

    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfROundPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("ShipLeave")]
        static void PrefixTeleportPlayer(StartOfRound __instance)
        {
            SharedData.Instance.BrackenRoomPosition = null;
            SharedData.FlushMaps();
        }
    }
}