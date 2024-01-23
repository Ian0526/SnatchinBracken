using HarmonyLib;
using SnatchinBracken.Patches.data;

namespace SnatchingBracken.Patches.ship
{

    [HarmonyPatch(typeof(ItemDropship))]
    internal class ItemDropshipPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("ShipLeave")]
        static void PrefixTeleportPlayer(ItemDropship __instance)
        {
            SharedData.Instance.BrackenRoomPosition = null;
            SharedData.FlushMaps();
        }
    }
}