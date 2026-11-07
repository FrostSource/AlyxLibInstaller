using AlyxLib;
using AlyxLib.Logging;
using AlyxLib.Services;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.Input;
using Source2HelperLibrary;
using System.Diagnostics;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    private FolderDeletionMonitor? _alyxLibMonitor;

    public bool AlyxLibExists => AlyxLibInstance.AlyxLibExists;

    public bool TrySetAlyxLibPath(string path)
    {
        if (!AlyxLibHelpers.CheckPathIsAlyxLib(path)) return false;

        _alyxLibMonitor?.Dispose();

        AlyxLibInstance.SetAlyxLibPath(path);
        Settings.AlyxLibDirectory = path;
        OnPropertyChanged(nameof(IsInstallationReady));
        OnPropertyChanged(nameof(AlyxLibExists));

        _alyxLibMonitor = FolderDeletionMonitor.Either(path, () =>
        {
            OnPropertyChanged(nameof(IsInstallationReady));
            OnPropertyChanged(nameof(AlyxLibExists));
        });

        return true;
    }

    /// <summary>
    /// Asks the user to download AlyxLib or select an existing AlyxLib folder.
    /// </summary>
    private async Task ShowAlyxLibSetupPopup()
    {
        var response = await _dialogService.ShowTextPopup(new DialogConfiguration
        {
            Title = "Setup AlyxLib",
            Message = "The AlyxLib files must exist on your computer so they can be linked to your addon, and it is recommended that you download to the same folder your addons exist in.\n\nWould you like to select a download location or select an already downloaded AlyxLib?",
            PrimaryButtonText = "Download AlyxLib",
            SecondaryButtonText = "Select AlyxLib Folder",
            CancelButtonText = "Cancel",
            Width = 500
        });

        switch (response.Result)
        {
            case DialogResult.Primary:
                await _updateService.DownloadAlyxLib();
                break;
            case DialogResult.Secondary:
                PickAlyxLibPath();
                break;
        }
    }

    [RelayCommand]
    private void PickAlyxLibPath(object? ownerWindow = null)
    {
        var folder = _dialogService.PickFolder("Select AlyxLib folder", HLA.GetAddonContentFolder());

        if (folder == null)
        {
            _logger?.LogDetail("User cancelled AlyxLib folder selection.");
            return;
        }

        var name = Path.GetFileName(folder);
        if (name == null)
        {
            _logger?.LogError($"Path is invalid: {folder}");
            return;
        }

        if (!TrySetAlyxLibPath(folder))
        {
            _logger?.LogError($"'{folder}' is not a valid AlyxLib folder!");
            _dialogService.ShowWarningPopup("Invalid AlyxLib folder", $"'{folder}' is not a valid AlyxLib folder!");
            return;
        }

        _logger?.Log($"AlyxLib path set to '{folder}'", LogType.Success);
    }

    [RelayCommand]
    private void OpenAlyxLibPath()
    {
        if (!AlyxLibExists)
        {
            _dialogService.ShowWarningPopup("AlyxLib path is not set!");
            return;
        }

        if (AlyxLibInstance.AlyxLibPath == null || !Directory.Exists(AlyxLibInstance.AlyxLibPath.FullName))
        {
            _logger?.LogError("AlyxLib path does not exist. Please set AlyxLib path.");
            _dialogService.ShowWarningPopup("AlyxLib path does not exist. Please set AlyxLib path.");
            return;
        }

        _logger?.LogDetail($"Opening AlyxLib path: {AlyxLibInstance.AlyxLibPath.FullName}");
        Process.Start("explorer.exe", AlyxLibInstance.AlyxLibPath.FullName);
    }

    [RelayCommand]
    private Task DownloadAlyxLib() => _updateService.DownloadAlyxLib(newLocation: true);

    [RelayCommand]
    private Task CheckForUpdates() => _updateService.CheckForUpdates();

    public Task<VersionComparisonResult> GetAlyxLibVersionComparison() => _updateService.GetVersionComparison();
}
