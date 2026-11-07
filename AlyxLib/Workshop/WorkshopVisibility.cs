namespace AlyxLib.Workshop;

/// <summary>
/// Workshop item visibility. Values match Steam's ERemoteStoragePublishedFileVisibility.
/// </summary>
public enum WorkshopVisibility
{
    Public = 0,
    FriendsOnly = 1,
    Private = 2,
    Unlisted = 3
}
