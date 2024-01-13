using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SnatchinBracken.Patches;

namespace SnatchinBracken
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class SnatchinBrackenBase : BaseUnityPlugin
    {
        private const string modGUID = "Ovchinikov.SnatchingBracken";
        private const string modName = "SnatchingBracken";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static SnatchinBrackenBase instance;

        internal ManualLogSource mls;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("Enabling Snatching Bracken");

            harmony.PatchAll(typeof(SnatchinBrackenBase));
            harmony.PatchAll(typeof(BrackenAIPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
        }
    }
}
