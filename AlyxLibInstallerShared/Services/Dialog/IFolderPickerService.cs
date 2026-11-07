namespace AlyxLibInstallerShared.Services.Dialog;
public interface IFolderPickerService
{
    string? PickFolder(string? title = null, string? initialPath = null);

    Task<string?> PickFolderAsync(string? title = null, string? initialPath = null);

    /// <param name="filter">A file dialog filter, e.g. "Text files (*.txt)|*.txt".</param>
    string? PickFile(string? title = null, string? filter = null, string? initialPath = null);
}
