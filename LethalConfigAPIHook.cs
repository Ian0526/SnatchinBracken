using BepInEx.Configuration;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using LethalConfig;
using SnatchinBracken.Patches.data;
using SnatchinBracken;

namespace SnatchingBracken
{
    internal class LethalConfigAPIHook
    {

        public static void InitializeConfig()
        {
            LethalConfigManager.SetModDescription("A mod that alters the behavior of the Bracken. The Bracken pulls players into a new spot before performing a kill. DON'T CHANGE SETTINGS WHILE THE BRACKEN IS ACTIVELY GRABBING!");

            // Error handler to check if HUDManager instance is valid
            if (HUDManager.Instance == null)
            {
                LethalConfigManager.SetModDescription("HUDManager instance is null. Config changes may not work as expected.");
                return;
            }

            // Handle setting change for host or server with action
            void HandleSettingChange<T>(ConfigEntry<T> entry, Action onHostChange)
            {
                entry.SettingChanged += delegate
                {
                    if (HUDManager.Instance.IsHost || HUDManager.Instance.IsServer)
                    {
                        onHostChange();
                    }
                };
            }

            // Should players drop items on grab
            ConfigEntry<bool> dropItemsOption = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Drop Items on Snatch", true, "Should players drop their items when a Bracken grabs them?");
            BoolCheckBoxConfigItem dropItemsVal = new BoolCheckBoxConfigItem(dropItemsOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)dropItemsVal);
            SharedData.Instance.DropItems = dropItemsOption.Value;
            HandleSettingChange(dropItemsOption, () => SharedData.Instance.DropItems = dropItemsOption.Value);

            // Should players be ignored from Turrets
            ConfigEntry<bool> turretOption = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Ignore Turrets on Snatch", true, "Should players be ignored by turrets when dragged?");
            BoolCheckBoxConfigItem turretVal = new BoolCheckBoxConfigItem(turretOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)turretVal);
            SharedData.Instance.IgnoreTurrets = turretOption.Value;
            HandleSettingChange(turretOption, () => SharedData.Instance.IgnoreTurrets = turretOption.Value);

            // Should Brackens automatically kill players when stuck
            ConfigEntry<bool> stuckForceKillOption = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Stuck Force Kill", false, "If enabled, Brackens will force kill when stuck at the same spot for at least 5 seconds.");
            BoolCheckBoxConfigItem stuckForceKillVal = new BoolCheckBoxConfigItem(stuckForceKillOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)stuckForceKillVal);
            SharedData.Instance.StuckForceKill = stuckForceKillOption.Value;
            HandleSettingChange(stuckForceKillOption, () => SharedData.Instance.StuckForceKill = stuckForceKillOption.Value);

            // Set Bracken's favorite position at the Bracken Room
            ConfigEntry<bool> brackenRoomOption = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Force Set Favorite Location To Bracken Room", true, "If enabled, Brackens' favorite locations will be set to the Bracken room.");
            BoolCheckBoxConfigItem brackenRoomVal = new BoolCheckBoxConfigItem(brackenRoomOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)brackenRoomVal);
            SharedData.Instance.BrackenRoom = brackenRoomOption.Value;
            HandleSettingChange(brackenRoomOption, () => SharedData.Instance.BrackenRoom = brackenRoomOption.Value);

            // Should people be able to teleport if being dragged?
            ConfigEntry<bool> allowDraggedTps = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Allow teleports to save dragged players", true, "Should players be able to be saved through teleportation?");
            BoolCheckBoxConfigItem allowDraggedTpsVal = new BoolCheckBoxConfigItem(allowDraggedTps);
            LethalConfigManager.AddConfigItem((BaseConfigItem)allowDraggedTpsVal);
            SharedData.Instance.AllowTeleports = allowDraggedTps.Value;
            HandleSettingChange(allowDraggedTps, () => SharedData.Instance.AllowTeleports = allowDraggedTps.Value);

            // Should players & Brackens ignore landmines
            ConfigEntry<bool> mineOption = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Ignore Mines on Snatch", true, "Should players ignore Landmines while being dragged?");
            BoolCheckBoxConfigItem mineVal = new BoolCheckBoxConfigItem(mineOption);
            LethalConfigManager.AddConfigItem((BaseConfigItem)mineVal);
            SharedData.Instance.IgnoreMines = mineOption.Value;
            HandleSettingChange(mineOption, () => SharedData.Instance.IgnoreMines = mineOption.Value);

            // Should enemies ignore dragged players?
            ConfigEntry<bool> monstersIgnorePlayersOption = SnatchinBrackenBase.Instance.Config.Bind<bool>("SnatchinBracken Settings", "Enemies Ignore Dragged Players", true, "Should players be ignored by other monsters while being dragged?");
            BoolCheckBoxConfigItem monstersIgnoreVal = new BoolCheckBoxConfigItem(monstersIgnorePlayersOption);
            LethalConfigManager.AddConfigItem(monstersIgnoreVal);
            SharedData.Instance.MonstersIgnorePlayers = monstersIgnorePlayersOption.Value;
            HandleSettingChange(monstersIgnorePlayersOption, () => SharedData.Instance.MonstersIgnorePlayers = monstersIgnorePlayersOption.Value);

            // Percent chance for Bracken to insta kill
            ConfigEntry<int> instaKillPercentEntry = SnatchinBrackenBase.Instance.Config.Bind<int>("SnatchinBracken Settings", "Chance for Insta Kill", 0, "Percent chance for insta kill, 0 to disable.");
            IntSliderOptions instaKillPercentOptions = new IntSliderOptions { RequiresRestart = false, Min = 0, Max = 100 };
            IntSliderConfigItem instaKillTimeSlider = new IntSliderConfigItem(instaKillPercentEntry, instaKillPercentOptions);
            LethalConfigManager.AddConfigItem((BaseConfigItem)instaKillTimeSlider);
            SharedData.Instance.PercentChanceForInsta = instaKillPercentEntry.Value;
            HandleSettingChange(instaKillPercentEntry, () => SharedData.Instance.PercentChanceForInsta = instaKillPercentEntry.Value);

            // Additional settings omitted for brevity but follow the same pattern...
        }
    }
}
