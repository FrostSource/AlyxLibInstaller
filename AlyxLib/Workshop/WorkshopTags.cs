namespace AlyxLib.Workshop;

/// <param name="SingleChoice">Only one of the group's tags is chosen, like the Workshop tools' dropdowns.</param>
public record WorkshopTagGroup(string Name, IReadOnlyList<string> Tags, bool SingleChoice = false);

/// <summary>
/// The Half-Life: Alyx Workshop tags, as offered by the Workshop tools.
/// </summary>
public static class WorkshopTags
{
    /// <summary>
    /// The item type tag the Workshop tools add to every addon. It's kept on every item and isn't shown as a choice.
    /// </summary>
    public const string AlyxAddon = "Alyx Addon";

    public static readonly IReadOnlyList<WorkshopTagGroup> Groups =
    [
        new("Addon Type", ["Maps", "Game Modifications & Replacements", "Developer Resources", "Other Addon Type"], SingleChoice: true),
        new("Game Content", ["Asset Packs", "Prefabs", "NPCs", "Weapons", "Models", "Textures", "Sounds", "Particles",
            "Quality Of Life", "Custom Mechanics", "Scripts & AI", "Miscellaneous Content"]),
        new("Gameplay Genre", ["HL:A Campaign-like", "Action", "Puzzle", "Showcase & Scenic", "Platforming & Climb", "Adventure",
            "Stealth", "RPG", "Arcade & Minigames", "Sandbox", "Other Genre"]),
        new("Environment", ["Urban", "Xenfestation", "Combine Structures", "Rural", "Nature", "Industrial", "Underground",
            "Exotic", "Snow", "Water", "Other Environment"]),
        new("Theme", ["Valve Games", "Recreations & Remakes", "Ported Content", "Open-World", "Defense & Arenas", "Vehicle",
            "Horror", "Sci-fi", "Pop Culture", "Comedy", "Fantasy", "Musical", "Crime", "Historical", "Seasonal", "Training",
            "Educational", "Other Theme"]),
        new("Enemies", ["Combine Soldiers", "Headcrabs", "Zombies", "Antlions", "Jeff", "Friendly Companions", "Other Enemies"]),
        new("Length", ["Short (less than 20m)", "Long (an hour)", "Campaign (2 hours or more)", "Infinite / Looped"], SingleChoice: true),
        new("Difficulty", ["No Combat", "Beginner", "Hardcore"]),
    ];
}
