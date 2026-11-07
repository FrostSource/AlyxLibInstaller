using AlyxLib;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    private Timer? _fileRemovalCountTimer;

    /// <summary>
    /// Updates the file removal count once game folder changes settle, so a compile writing hundreds
    /// of files doesn't rescan the whole folder for each one.
    /// </summary>
    private void QueueFileRemovalCountUpdate()
    {
        _fileRemovalCountTimer ??= new Timer(_ => OnPropertyChanged(nameof(FileRemovalCount)));
        _fileRemovalCountTimer.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileRemovalCount))]
    public partial FileGlobCollection FileRemovalGlobCollection { get; set; }

    public int FileRemovalCount => SelectedAddon == null ? 0 : FileRemovalGlobCollection.GetMatchingFileCount(SelectedAddon.GamePath);

    private static List<string> SerializeFileRemovalGlobs(IEnumerable<EditableEntry> entries) =>
        [.. entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .Select(entry => entry.ToConfigString())];

    [RelayCommand]
    private async Task ShowFileRemovalEditor()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot show file removal editor");
            return;
        }

        var globClone = FileRemovalGlobCollection.Clone();
        var response = await _dialogService.ShowFileRemovalPopup(new DialogConfiguration
        {
            Title = "Manage Removal List",
            PrimaryButtonText = "Save",
            CancelButtonText = "Cancel",
        }, globClone, SelectedAddon);

        if (response.Result != DialogResult.Primary)
        {
            _logger?.LogDetail("File removal editor cancelled");
            return;
        }

        FileRemovalGlobCollection = globClone;
        var serializedGlobs = SerializeFileRemovalGlobs(FileRemovalGlobCollection);
        // Also edit addon config on disk
        var diskConfig = AlyxLibHelpers.GetAddonConfig(SelectedAddon);
        diskConfig.FileRemovalGlobs = serializedGlobs;
        AlyxLibInstance.FileManager.SaveAddonConfig(SelectedAddon, diskConfig);
        // Update globs in memory
        AddonConfig.FileRemovalGlobs = serializedGlobs;

        _logger?.LogDetail("File removal list updated");
    }

    [RelayCommand]
    private async Task ShowFileRemovalFiles()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot show file removal list");
            return;
        }

        await ShowFileListDialog(FileRemovalGlobCollection.GetMatchingFiles(SelectedAddon.GamePath));
    }

    public async Task ShowFileListDialog(IEnumerable<string> list)
    {
        await _dialogService.ShowListPopup(new DialogConfiguration
        {
            Title = "File Removal List",
            Message = "These files match your upload removal rules. The legend shows which files are deleted and which are restored on the next Install.",
            CancelButtonText = "Close",
        }, list);
    }
}
