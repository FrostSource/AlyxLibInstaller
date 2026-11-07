namespace AlyxLib.Workshop;

/// <summary>
/// An item's properties as Steam currently has them, including any edits made on the Workshop page.
/// </summary>
public record WorkshopItemDetails(
    ulong ItemId,
    string Title,
    string Description,
    WorkshopVisibility Visibility,
    IReadOnlyList<string> Tags,
    IReadOnlyList<ulong> RequiredItems,
    string PreviewUrl,
    DateTimeOffset Updated);
