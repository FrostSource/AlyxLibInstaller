namespace AlyxLib.Workshop;

/// <summary>
/// The contents of a publish_data.txt file written next to a published addon's VPK.
/// </summary>
/// <param name="ItemId">The Workshop item ID, which is also the name of the folder holding the file.</param>
/// <param name="SourceFolder">The name of the addon folder the item was packed from.</param>
public record PublishData(ulong ItemId, string Title, string SourceFolder, DateTimeOffset PublishTime);
