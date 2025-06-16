using GenericModConfigMenu;
using StardewModdingAPI;

namespace Exbrain.DisplayLuck;

internal sealed class ModConfig
{
    public SButton ShowInfoButton { get; set; } = SButton.F3;

    public SButton CalendarButton { get; set; } = SButton.F8;

    public SButton BillboardButton { get; set; } = SButton.F9;

    public SButton SpecialOrdersButton { get; set; } = SButton.F10;

    public SButton QiOrdersButton { get; set; } = SButton.F11;

    public SButton StackButton { get; set; } = SButton.N;

    public int DebugPosX { get; set; } = 0;

    public int DebugPosY { get; set; } = 0;

    public int StackChestDistance { get; set; } = 15;

    public bool ShowSkillMissingXp { get; set; } = false;

    public bool ShareSkillXp { get; set; } = false;

    public static void CreateMenu(IModHelper helper, IManifest manifest, Func<ModConfig> getConfig, Action<ModConfig> setConfig)
    {
        var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null) return;

        configMenu.Register(
            mod: manifest,
            reset: () => setConfig(new ModConfig()),
            save: () => helper.WriteConfig(getConfig())
        );
        configMenu.AddBoolOption(
            mod: manifest,
            name: () => "Show Skills Missing XP",
            tooltip: () => "Whether to show missing skills XP or not.",
            getValue: () => getConfig().ShowSkillMissingXp,
            setValue: (value) => getConfig().ShowSkillMissingXp = value
        );
        configMenu.AddBoolOption(
            mod: manifest,
            name: () => "Share Skills XP",
            tooltip: () => "Whether the xp of skills is shared between them.",
            getValue: () => getConfig().ShareSkillXp,
            setValue: (value) => getConfig().ShareSkillXp = value
        );
        configMenu.AddKeybind(
            mod: manifest,
            name: () => "Show Info Button",
            tooltip: () => "The button to toggle the information box.",
            getValue: () => getConfig().ShowInfoButton,
            setValue: (value) => getConfig().ShowInfoButton = value
        );
        configMenu.AddKeybind(
            mod: manifest,
            name: () => "Calendar Button",
            tooltip: () => "The button to open the calendar.",
            getValue: () => getConfig().CalendarButton,
            setValue: (value) => getConfig().CalendarButton = value
        );
        configMenu.AddKeybind(
            mod: manifest,
            name: () => "Billboard Button",
            tooltip: () => "The button to open the billboard.",
            getValue: () => getConfig().BillboardButton,
            setValue: (value) => getConfig().BillboardButton = value
        );
        configMenu.AddKeybind(
            mod: manifest,
            name: () => "Special Orders Button",
            tooltip: () => "The button to open the special orders.",
            getValue: () => getConfig().SpecialOrdersButton,
            setValue: (value) => getConfig().SpecialOrdersButton = value
        );
        configMenu.AddKeybind(
            mod: manifest,
            name: () => "Qi Orders Button",
            tooltip: () => "The button to open the Qi orders.",
            getValue: () => getConfig().QiOrdersButton,
            setValue: (value) => getConfig().QiOrdersButton = value
        );
        configMenu.AddKeybind(
            mod: manifest,
            name: () => "Stack to nearby chests Button",
            tooltip: () => "The button to stack items to nearby chests.",
            getValue: () => getConfig().StackButton,
            setValue: (value) => getConfig().StackButton = value
        );
        configMenu.AddNumberOption(
            mod: manifest,
            name: () => "Stack radius",
            tooltip: () => "Stack items distance from player to chest in tiles.\n(0 for an infinite distance)",
            getValue: () => getConfig().StackChestDistance,
            setValue: (value) => getConfig().StackChestDistance = value,
            min: 0,
            max: 100
        );

    }
}
