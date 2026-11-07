using AlyxLib;
using AlyxLib.Logging;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlyxLibInstallerShared.ViewModels;

/// <summary>
/// Main view model for the installer. Split across partial files by area:
/// themes, addons, file removal, install, AlyxLib and shell.
/// </summary>
public partial class AlyxLibInstallerSharedViewModel : ObservableRecipient
{
    private readonly IDialogService _dialogService;
    private readonly ILogger? _logger;
    private readonly AddonInstallService _installService;
    private readonly AlyxLibUpdateService _updateService;

    private void LogException(Exception ex, bool quiet = false)
    {
        if (!quiet)
            _logger?.LogError($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}");

        FileLogger.Log(ex);
    }

    public AlyxLibManager AlyxLibInstance { get; } = new AlyxLibManager();

    public Settings Settings { get; }

    [ObservableProperty]
    public partial bool UserHasSymlinkPrivileges { get; set; } = false;

    [ObservableProperty]
    public partial bool IsInitializing { get; set; } = true;

    /// <summary>
    /// Indicates whether the installer is working on something and the UI should be blocked.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCurrentlyLoading { get; set; } = false;
    [ObservableProperty]
    public partial string LoadingOverlayText { get; set; } = "";
    private void SetLoading(bool isLoading, string text = "") => (IsCurrentlyLoading, LoadingOverlayText) = (isLoading, text);

    public AlyxLibInstallerSharedViewModel(IDialogService dialogService, ILogger? logger = null)
    {
        Settings = SettingsManager.Settings;
        Settings.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.RememberLastAddon):
                    Settings.LastAddon = Settings.RememberLastAddon ? SelectedAddon?.Name ?? string.Empty : string.Empty;
                    break;
            }
        };
        SettingsManager.ErrorOccurred += (_, ex) => LogException(ex);

        _dialogService = dialogService;
        _logger = logger;
        AlyxLibInstance.Logger = _logger;

        _installService = new AddonInstallService(_dialogService, new AddonFileOperations(AlyxLibInstance.FileManager), _logger);
        _updateService = new AlyxLibUpdateService(_dialogService, AlyxLibInstance, Settings, TrySetAlyxLibPath, SetLoading, _logger);

        FileRemovalGlobCollection = new FileGlobCollection();
        FileRemovalGlobCollection.CollectionChanged += (_, _) =>
        {
            if (AddonConfig == null)
            {
                _logger?.LogError("Trying to update file removal list when no addon is selected!");
                return;
            }

            AddonConfig.FileRemovalGlobs = SerializeFileRemovalGlobs(FileRemovalGlobCollection);
            OnPropertyChanged(nameof(FileRemovalCount));
        };
    }

    public async Task InitializeAsync()
    {
        IsInitializing = true;

        try
        {
            await InitializeCoreAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Startup did not complete.");
            LogException(ex);
        }
        finally
        {
            // Normally already false, this makes sure the UI isn't left hidden if startup fails early
            IsInitializing = false;
        }
    }

    private async Task InitializeCoreAsync()
    {
        InitializeThemes();

        SettingsManager.Save();

        // Registers the view model with the messenger
        IsActive = true;

        _logger?.Log($"Settings file: {SettingsManager.Path}");
        _logger?.Log($"Log file: {FileLogger.LogFilePath}");

        CleanupLogFiles();

        InitializeAddons();

        // Show the main UI before any startup dialogs so it isn't blank behind them
        IsInitializing = false;

        if (string.IsNullOrEmpty(Settings.AlyxLibDirectory) || !TrySetAlyxLibPath(Settings.AlyxLibDirectory))
            await ShowAlyxLibSetupPopup();

        UserHasSymlinkPrivileges = PrivilegeChecker.CanCreateSymlinks();
        if (!UserHasSymlinkPrivileges)
            await _dialogService.ShowPrivilegeWarning();

        await _updateService.CheckForUpdatesOnStartup();
    }

    /// <summary>
    /// Deletes old log files, or warns about them if auto delete is turned off.
    /// </summary>
    private void CleanupLogFiles()
    {
        var logFileCount = FileLogger.LogFileCount;
        if (logFileCount <= Settings.MaxLogFiles)
            return;

        if (Settings.AutoDeleteLogFiles)
        {
            var oldFileCount = logFileCount - Settings.MaxLogFiles;
            _logger?.LogDetail($"Deleting {oldFileCount} old log file{(oldFileCount == 1 ? "" : "s")}...");
            FileLogger.CleanupOldLogs(Settings.MaxLogFiles);
        }
        else
        {
            string logsPath = Path.GetDirectoryName(FileLogger.LogFilePath) ?? "";
            _logger?.LogWarning(
                $"There are currently {logFileCount} log files in your logs folder.\n" +
                $"You may want to clean them up to save disk space.\n" +
                $"Logs folder: {logsPath}");
        }
    }
}
