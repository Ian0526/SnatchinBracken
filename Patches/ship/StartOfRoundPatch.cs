using HarmonyLib;
using SnatchinBracken.Patches.data;

namespace SnatchingBracken.Patches.ship
{

    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("ShipLeave")]
        static void PrefixShipLeave(StartOfRoundPatch __instance)
        {
            SharedData.Instance.BrackenRoomPosition = null;
            SharedData.FlushDictionaries();
            SharedData.GiveChillPillToAll();
        }

        [HarmonyPrefix]
        [HarmonyPatch("openingDoorsSequence")]
        static void ClearPlayerSanityOnLand(StartOfRound __instance)
        {
            SharedData.GiveChillPillToAll();
        }
    }
}