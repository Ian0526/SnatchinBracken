using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using SnatchinBracken.Patches.data;

namespace SnatchingBracken.Patches.dungeon
{

    [HarmonyPatch(typeof(DungeonGenerator))]
    internal class DungeonGenPatch
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Dungeon";

        private static ManualLogSource logger;

        static DungeonGenPatch()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        [HarmonyPatch("ChangeStatus")]
        [HarmonyPostfix]
        public static void OnChangeStatus(DungeonGenerator __instance)
        {
            if (__instance.CurrentDungeon == null)
            {
                logger.LogInfo("CurrentDungeon is null");
            }
            else if (__instance.CurrentDungeon.AllTiles == null)
            {
                logger.LogInfo("AllTiles is null");
            }
            Tile tile = FindTileWithName(__instance.CurrentDungeon, "SmallRoom2");
            if (tile != null)
            {
                SharedData.Instance.BrackenRoomPosition = tile.transform;
                logger.LogInfo("We found the Bracken room tile at: " + tile.name);
            }
        }

        public static Tile FindTileWithName(Dungeon dungeon, string nameContains)
        {
            if (dungeon == null)
            {
                logger.LogError("Dungeon is null");
                return null;
            }

            foreach (Tile tile in dungeon.AllTiles)
            {
                if (tile.name.Contains(nameContains))
                {
                    return tile;
                }
            }

            return null;
        }
    }
}
