namespace AlyxLib.Workshop;

/// <summary>
/// A connection to the Steam Workshop. Dispose it when done so Steam stops showing the game as running.
/// </summary>
public interface IWorkshopClient : IDisposable
{
    /// <summary>
    /// Gets an item's current details, skipping Steam's cache so edits made on the Workshop page are included.
    /// </summary>
    /// <returns>Null if the item doesn't exist.</returns>
    /// <exception cref="WorkshopException">Steam couldn't get the item, or it belongs to another account.</exception>
    Task<WorkshopItemDetails?> GetItemAsync(ulong itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits an update. Only the fields set in <paramref name="update"/> are changed.
    /// </summary>
    /// <exception cref="WorkshopException">Steam rejected the update.</exception>
    Task<WorkshopSubmitResult> SubmitUpdateAsync(WorkshopItemUpdate update, IProgress<WorkshopUploadProgress>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Changes to make to a Workshop item. A null field is left as it is on the Workshop.
/// </summary>
public record WorkshopItemUpdate(ulong ItemId)
{
    /// <summary>
    /// The folder to upload as the item's content, holding the VPK and publish_data.txt.
    /// </summary>
    public string? ContentFolder { get; init; }
    public string? ChangeNote { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public WorkshopVisibility? Visibility { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public IReadOnlyList<ulong>? RequiredItems { get; init; }
    public string? PreviewFile { get; init; }

    /// <summary>
    /// Adds the local values of the given properties to the update.
    /// </summary>
    /// <param name="description">The local description, already read from the linked file if there is one.</param>
    public WorkshopItemUpdate WithProperties(WorkshopProperties properties, string? description, WorkshopFields fields) => this with
    {
        Title = fields.HasFlag(WorkshopFields.Title) ? properties.Title : Title,
        Description = fields.HasFlag(WorkshopFields.Description) ? description : Description,
        Visibility = fields.HasFlag(WorkshopFields.Visibility) ? properties.Visibility : Visibility,
        Tags = fields.HasFlag(WorkshopFields.Tags) ? properties.Tags : Tags,
        RequiredItems = fields.HasFlag(WorkshopFields.RequiredItems) ? properties.RequiredItems : RequiredItems
    };
}

/// <param name="NeedsLegalAgreement">The update was saved, but stays hidden until the user accepts the Workshop legal agreement.</param>
public record WorkshopSubmitResult(bool NeedsLegalAgreement);

/// <param name="Fraction">How much of the current stage is done, or null if Steam doesn't report it.</param>
public record WorkshopUploadProgress(string Stage, double? Fraction);

public class WorkshopException(string message) : Exception(message);
