using BepInEx.Configuration;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using SnatchinBracken.Patches.data;
using SnatchinBracken;
using BepInEx;

namespace SnatchingBracken
{
    internal class LethalConfigAPIHook
    {

        public static void InitializeConfig()
        {
            LethalConfigManager.SetModDescription("A mod that alters the behavior of the Bracken. The Bracken pulls players into a new spot before performing a kill. NONE OF THESE OPTIONS REQUIRE A RESTART!");

            // Should players drop items on grab
            ConfigEntry<bool> dropItemsOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Drop Items on Snatch", true, "Should players drop their items when a Bracken grabs them?"));
            BoolCheckBoxConfigItem dropItemsVal = new BoolCheckBoxConfigItem(dropItemsOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)dropItemsVal);
            SharedData.Instance.DropItems = dropItemsOption.Value;
            dropItemsOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.DropItems = dropItemsOption.Value;
                }
            };

            // Should players be ignored from Turrets
            ConfigEntry<bool> turretOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Ignore Turrets on Snatch", true, "Should players be ignored by turrets when dragged?"));
            BoolCheckBoxConfigItem turretVal = new BoolCheckBoxConfigItem(turretOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)turretVal);
            SharedData.Instance.IgnoreTurrets = turretOption.Value;
            turretOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.IgnoreTurrets = turretOption.Value;
                }
            };

            // Should Brackens behave more naturally, meaning faster, more chaotic deaths
            ConfigEntry<bool> chaoticOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Brackens Behave More Naturally", false, "If enabled, Brackens will perform kills at unpredictable times after an initial drop. Otherwise, the Bracken either must be in distance of the favorite location, or hit the time limit."));
            BoolCheckBoxConfigItem chaoticVal = new BoolCheckBoxConfigItem(chaoticOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)chaoticVal);
            SharedData.Instance.ChaoticTendencies = chaoticOption.Value;
            chaoticOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.ChaoticTendencies = chaoticOption.Value;
                }
            };

            // Add a new configuration option for stuckForceKill
            ConfigEntry<bool> stuckForceKillOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Stuck Force Kill", false, "If enabled, Brackens will force kill when stuck at the same spot for at least 5 seconds."));
            BoolCheckBoxConfigItem stuckForceKillVal = new BoolCheckBoxConfigItem(stuckForceKillOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)stuckForceKillVal);
            SharedData.Instance.StuckForceKill = stuckForceKillOption.Value;

            // Handle the event when the setting is changed
            stuckForceKillOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.StuckForceKill = stuckForceKillOption.Value;
                }
            };

            ConfigEntry<bool> brackenRoomOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Force Set Favorite Location To Bracken Room", true, "If enabled, Brackens' favorite locations will be set to the Bracken room. The room sometimes doesn't spawn, so please don't be alarmed if they don't take you there if this is enabled."));
            BoolCheckBoxConfigItem brackenRoomVal = new BoolCheckBoxConfigItem(brackenRoomOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)brackenRoomVal);
            SharedData.Instance.BrackenRoom = brackenRoomOption.Value;

            brackenRoomOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.BrackenRoom = brackenRoomOption.Value;
                }
            };

            // Should people be able to teleported if they're being dragged?
            ConfigEntry<bool> allowDraggedTps = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Allow teleports to save dragged players", true, "Should players be able to be saved through teleportation?"));
            BoolCheckBoxConfigItem allowDraggedTpsVal = new BoolCheckBoxConfigItem(allowDraggedTps);
            LethalConfigManager.AddConfigItem((BaseConfigItem)allowDraggedTpsVal);
            SharedData.Instance.AllowTeleports = allowDraggedTps.Value;
            allowDraggedTps.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.AllowTeleports = allowDraggedTps.Value;
                }
            };

            // Should players ignore Landmines
            ConfigEntry<bool> mineOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Ignore Mines on Snatch", true, "Should players ignore Landmines while being dragged?"));
            BoolCheckBoxConfigItem mineVal = new BoolCheckBoxConfigItem(mineOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)mineVal);
            SharedData.Instance.IgnoreMines = mineOption.Value;
            mineOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.IgnoreMines = mineOption.Value;
                }
            };

            // Should players ignore Landmines
            // Players
            ConfigEntry<bool> monstersIgnorePlayersOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Enemies Ignore Dragged Players", true, "Should players be ignored by other monsters while being dragged?"));
            BoolCheckBoxConfigItem monstersIgnoreVal = new BoolCheckBoxConfigItem(monstersIgnorePlayersOption);
            SharedData.Instance.MonstersIgnorePlayers = monstersIgnorePlayersOption.Value;
            LethalConfigManager.AddConfigItem(monstersIgnoreVal);
            monstersIgnorePlayersOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.MonstersIgnorePlayers = monstersIgnorePlayersOption.Value;
                }
            };

            // Slider for seconds until Bracken automatically kills when grabbed
            ConfigEntry<int> instaKillTimeEntry = (SnatchinBrackenBase.Instance.Config.Bind<int>("SnatchinBracken Settings", "Chance for Insta Kill", 0, "Percent chance for insta kill, 0 to disable."));
            IntSliderOptions instaKillTimeOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            };
            IntSliderConfigItem instaKillTimeSlider = new IntSliderConfigItem(instaKillTimeEntry, instaKillTimeOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)instaKillTimeSlider);
            SharedData.Instance.PercentChanceForInsta = instaKillTimeEntry.Value;
            instaKillTimeEntry.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.PercentChanceForInsta = instaKillTimeEntry.Value;
                }
            };

            // Slider for seconds until Bracken automatically kills when grabbed
            ConfigEntry<int> brackenKillTimeEntry = (SnatchinBrackenBase.Instance.Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Auto Kill", 15, "Time in seconds until Bracken automatically kills when grabbed. Range: 1-60 seconds."));
            IntSliderOptions brackenKillTimeOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem brackenKillTimeSlider = new IntSliderConfigItem(brackenKillTimeEntry, brackenKillTimeOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)brackenKillTimeSlider);
            SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
            brackenKillTimeEntry.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.KillAtTime = brackenKillTimeEntry.Value;
                }
            };

            // Slider for seconds until Bracken can try to attack another person after dropping/being hit
            ConfigEntry<int> brackenNextAttemptEntry = (SnatchinBrackenBase.Instance.Config.Bind<int>("SnatchinBracken Settings", "Seconds Until Next Attempt", 5, "Time in seconds until Bracken is allowed to take another victim."));
            IntSliderOptions brackenNextAttemptOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem brackenNextAttemptSlider = new IntSliderConfigItem(brackenNextAttemptEntry, brackenNextAttemptOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)brackenNextAttemptSlider);
            SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
            brackenNextAttemptEntry.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.SecondsBeforeNextAttempt = brackenNextAttemptEntry.Value;
                }
            };

            // Should Brackens deal damage over time instead of abruptly killing them after they reach a spot?
            ConfigEntry<bool> doDamageOnIntervalEntry = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Do Gradual Damage", false, "Should players be hurt gradually while being dragged?"));
            BoolCheckBoxConfigItem doDamageOnInterval = new BoolCheckBoxConfigItem(doDamageOnIntervalEntry);
            LethalConfigManager.AddConfigItem(doDamageOnInterval);
            SharedData.Instance.DoDamageOnInterval = doDamageOnIntervalEntry.Value;
            doDamageOnIntervalEntry.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.DoDamageOnInterval = doDamageOnIntervalEntry.Value;
                }
            };

            // Time required for above entry
            ConfigEntry<int> damageDealtProgressively = (SnatchinBrackenBase.Instance.Config.Bind<int>("SnatchinBracken Settings", "Damage Dealt At Interval", 5, "This only applies if you have \"Do Gradual Damage\" enabled. While dragged, every second this configured amount of damage will be dealt to the player."));
            IntSliderOptions damageDealtProgressivelyOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 100
            };
            IntSliderConfigItem damageDealtProgressivelySlider = new IntSliderConfigItem(damageDealtProgressively, damageDealtProgressivelyOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)damageDealtProgressivelySlider);
            SharedData.Instance.DamageDealtAtInterval = damageDealtProgressively.Value;
            damageDealtProgressively.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.DamageDealtAtInterval = damageDealtProgressively.Value;
                }
            };

            // Slider for seconds until Bracken can try to attack another person after dropping/being hit
            ConfigEntry<int> distanceAutoKillerEntry = (SnatchinBrackenBase.Instance.Config.Bind<int>("SnatchinBracken Settings", "Distance For Kill", 1, "How far should the Bracken be from its favorite spot to initiate a kill?"));
            IntSliderOptions distanceAutoKillerOptions = new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 1,
                Max = 60
            };
            IntSliderConfigItem distanceAutoKillerSlider = new IntSliderConfigItem(distanceAutoKillerEntry, distanceAutoKillerOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)distanceAutoKillerSlider);
            SharedData.Instance.DistanceFromFavorite = distanceAutoKillerEntry.Value;
            distanceAutoKillerEntry.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.DistanceFromFavorite = distanceAutoKillerEntry.Value;
                }
            };

            // Should the Bracken instakill if the player is alone
            ConfigEntry<bool> instaKillOption = (SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Instakill When Alone", false, "Should players be instantly killed if they're alone?"));
            BoolCheckBoxConfigItem instaKillVal = new BoolCheckBoxConfigItem(instaKillOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)instaKillVal);
            SharedData.Instance.InstantKillIfAlone = instaKillOption.Value;
            instaKillOption.SettingChanged += delegate
            {
                if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                {
                    SharedData.Instance.InstantKillIfAlone = instaKillOption.Value;
                }
            };
        }
    }
}
