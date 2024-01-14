using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SnatchinBracken.Patches;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using SnatchinBracken.Patches.data;

namespace SnatchinBracken
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class SnatchinBrackenBase : BaseUnityPlugin
    {
        private const string modGUID = "Ovchinikov.SnatchingBracken.Main";
        private const string modName = "SnatchingBracken";
        private const string modVersion = "1.0.1";

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
            mls.LogInfo("Enabling SnatchinBracken");

            InitializeConfigValues();

            harmony.PatchAll(typeof(SnatchinBrackenBase));
            harmony.PatchAll(typeof(BrackenAIPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
            harmony.PatchAll(typeof(TeleporterPatch));

            mls.LogInfo("Finished Enabling SnatchinBracken");
        }

        private void InitializeConfigValues()
        {
            mls.LogInfo("Parsing SnatchinBracken config");
            // Should players drop items on grab
            LethalConfigManager.SetModDescription("A mod that alters the behavior of the Bracken. The Bracken pulls players into a new spot before per");
            ConfigEntry<bool> dropItemsOption = ((BaseUnityPlugin) this).Config.Bind<bool>("SnatchinBracken Settings", "Drop Items on Snatch", true, "Should players drop their items when a Bracken grabs them.");
            BoolCheckBoxConfigItem val = new BoolCheckBoxConfigItem(dropItemsOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)(object)val);
            SharedData.Instance.DropItems = dropItemsOption.Value;
            dropItemsOption.SettingChanged += delegate
            {
                SharedData.Instance.DropItems = dropItemsOption.Value;
            };

            // Slider for seconds until Bracken automatically kills when grabbed
            ConfigEntry<int> brackenKillTimeEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Auto Kill", 15, "Time in seconds until Bracken automatically kills when grabbed. Range: 1-60 seconds.");
            IntSliderOptions brackenKillTimeOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem brackenKillTimeSlider = new IntSliderConfigItem(brackenKillTimeEntry, brackenKillTimeOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem) (object) brackenKillTimeSlider);
            SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            brackenKillTimeEntry.SettingChanged += delegate
            {
                SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            };

            mls.LogInfo("Config finished parsing");
        }
    }
}
