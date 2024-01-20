using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SnatchinBracken.Patches;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using SnatchinBracken.Patches.data;
using RuntimeNetcodeRPCValidator;
using SnatchingBracken.Patches.network;
using System.Reflection;
using UnityEngine;
using GameNetcodeStuff;

namespace SnatchinBracken
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("NicholaScott.BepInEx.RuntimeNetcodeRPCValidator", BepInDependency.DependencyFlags.HardDependency)]
    public class SnatchinBrackenBase : BaseUnityPlugin
    {
        private const string modGUID = "Ovchinikov.SnatchinBracken.Main";
        private const string modName = "SnatchinBracken";
        private const string modVersion = "1.2.5";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static SnatchinBrackenBase instance;

        private NetcodeValidator netcodeValidator;

        internal ManualLogSource mls;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo("Enabling SnatchinBracken");

            InitializeConfigValues();

            harmony.PatchAll(typeof(SnatchinBrackenBase));
            harmony.PatchAll(typeof(BrackenAIPatch));
            harmony.PatchAll(typeof(EnemyAIPatch));
            harmony.PatchAll(typeof(TeleporterPatch));
            harmony.PatchAll(typeof(LandminePatch));
            harmony.PatchAll(typeof(TurretPatch));

            netcodeValidator = new NetcodeValidator(modGUID);
            netcodeValidator.PatchAll();
            netcodeValidator.BindToPreExistingObjectByBehaviour<FlowermanBinding, PlayerControllerB>();

            mls.LogInfo("Finished Enabling SnatchinBracken");
        }

        private void InitializeConfigValues()
        {
            mls.LogInfo("Parsing SnatchinBracken config");
            LethalConfigManager.SetModDescription("A mod that alters the behavior of the Bracken. The Bracken pulls players into a new spot before performing a kill.");

            // Should players drop items on grab
            ConfigEntry<bool> dropItemsOption = ((BaseUnityPlugin) this).Config.Bind<bool>("SnatchinBracken Settings", "Drop Items on Snatch", true, "Should players drop their items when a Bracken grabs them?");
            BoolCheckBoxConfigItem dropItemsVal = new BoolCheckBoxConfigItem(dropItemsOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem) dropItemsVal);
            SharedData.Instance.DropItems = dropItemsOption.Value;
            dropItemsOption.SettingChanged += delegate
            {
                SharedData.Instance.DropItems = dropItemsOption.Value;
            };

            // Should players be ignored from Turrets
            ConfigEntry<bool> turretOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Ignore Turrets on Snatch", true, "Should players be targetable when dragged?");
            BoolCheckBoxConfigItem turretVal = new BoolCheckBoxConfigItem(turretOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem) turretVal);
            SharedData.Instance.IgnoreTurrets = turretOption.Value;
            turretOption.SettingChanged += delegate
            {
                SharedData.Instance.IgnoreTurrets = turretOption.Value;
            };

            // Should Brackens behave more naturally, meaning faster, more chaotic deaths
            ConfigEntry<bool> chaoticOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Brackens Behave More Naturally", false, "If enabled, Brackens will perform kills at unpredictable times after an initial drop. Otherwise, the Bracken either must be in distance of the favorite location, or hit the time limit.");
            BoolCheckBoxConfigItem chaoticVal = new BoolCheckBoxConfigItem(chaoticOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)chaoticVal);
            SharedData.Instance.ChaoticTendencies = chaoticOption.Value;
            chaoticOption.SettingChanged += delegate
            {
                SharedData.Instance.ChaoticTendencies = chaoticOption.Value;
            };

            // Should players ignore Landmines
            ConfigEntry<bool> mineOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Ignore Mines on Snatch", true, "Should players ignore Landmines while being dragged?");
            BoolCheckBoxConfigItem mineVal = new BoolCheckBoxConfigItem(mineOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)mineVal);
            SharedData.Instance.IgnoreMines = mineOption.Value;
            mineOption.SettingChanged += delegate
            {
                SharedData.Instance.IgnoreMines = mineOption.Value;
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
            LethalConfigManager.AddConfigItem((BaseConfigItem) brackenKillTimeSlider);
            SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            brackenKillTimeEntry.SettingChanged += delegate
            {
                SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            };

            // Slider for seconds until Bracken can try to attack another person after dropping/being hit
            ConfigEntry<int> brackenNextAttemptEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Next Attempt", 5, "Time in seconds until Bracken is allowed to take another victim.");
            IntSliderOptions brackenNextAttemptOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem brackenDistanceSlider = new IntSliderConfigItem(brackenNextAttemptEntry, brackenNextAttemptOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem) brackenDistanceSlider);
            SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
            brackenNextAttemptEntry.SettingChanged += delegate
            {
                SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
            };

            // Slider for seconds until Bracken can try to attack another person after dropping/being hit
            ConfigEntry<int> distanceAutoKillerEntry = ((BaseUnityPlugin)this).Config.Bind<int>("SnatchinBracken Settings", "Distance For Kill", 5, "How far should the Bracken be from its favorite spot to initiate a kill?");
            IntSliderOptions distanceAutoKillerOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem distanceAutoKillerSlider = new IntSliderConfigItem(distanceAutoKillerEntry, distanceAutoKillerOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem) distanceAutoKillerSlider);
            SharedData.Instance.DistanceFromFavorite = distanceAutoKillerEntry.Value;
            distanceAutoKillerEntry.SettingChanged += delegate
            {
                SharedData.Instance.DistanceFromFavorite = distanceAutoKillerEntry.Value;
            };

            // Should the Bracken instakill if the player is alone
            ConfigEntry<bool> instaKillOption = ((BaseUnityPlugin)this).Config.Bind<bool>("SnatchinBracken Settings", "Instakill When Alone", true, "Should players be instantly killed if they're alone?");
            BoolCheckBoxConfigItem instaKillVal = new BoolCheckBoxConfigItem (instaKillOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)instaKillVal);
            SharedData.Instance.InstantKillIfAlone = instaKillOption.Value;
            instaKillOption.SettingChanged += delegate
            {
                SharedData.Instance.InstantKillIfAlone = instaKillOption.Value;
            };

            mls.LogInfo("Config finished parsing");
        }
    }
}
