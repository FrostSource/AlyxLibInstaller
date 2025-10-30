namespace AlyxLibInstallerShared.Services.Dialog;
public interface IFolderPickerService
{
    string? PickFolder(string? title = null, string? initialPath = null);

    Task<string?> PickFolderAsync(string? title = null, string? initialPath = null);
}
