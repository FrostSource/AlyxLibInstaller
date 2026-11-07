namespace AlyxLib.Workshop;

/// <summary>
/// The properties that can differ between the local copy and Steam.
/// The preview image isn't compared because Steam only returns a URL for it.
/// </summary>
[Flags]
public enum WorkshopFields
{
    None = 0,
    Title = 1,
    Description = 2,
    Visibility = 4,
    Tags = 8,
    RequiredItems = 16
}

public static class WorkshopPropertyComparer
{
    /// <summary>
    /// Gets the tracked local properties that differ from Steam's.
    /// </summary>
    /// <param name="localDescription">The local description, already read from the linked file if there is one.</param>
    public static WorkshopFields GetDifferences(WorkshopProperties local, string? localDescription, WorkshopItemDetails remote)
    {
        var differences = WorkshopFields.None;

        if (local.Title != null && local.Title.Trim() != remote.Title.Trim())
            differences |= WorkshopFields.Title;

        if (localDescription != null && NormalizeText(localDescription) != NormalizeText(remote.Description))
            differences |= WorkshopFields.Description;

        if (local.Visibility != null && local.Visibility != remote.Visibility)
            differences |= WorkshopFields.Visibility;

        if (local.Tags != null && !local.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(remote.Tags))
            differences |= WorkshopFields.Tags;

        if (local.RequiredItems != null && !local.RequiredItems.ToHashSet().SetEquals(remote.RequiredItems))
            differences |= WorkshopFields.RequiredItems;

        return differences;
    }

    /// <summary>
    /// Ignores line ending and trailing whitespace differences, which Steam doesn't keep reliably.
    /// </summary>
    private static string NormalizeText(string text) =>
        string.Join('\n', text.Replace("\r\n", "\n").Split('\n').Select(line => line.TrimEnd())).Trim();
}
