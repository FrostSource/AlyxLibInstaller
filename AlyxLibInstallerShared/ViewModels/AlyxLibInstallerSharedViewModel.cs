using AlyxLib;
using AlyxLib.Logging;
using AlyxLibInstallerShared.Messaging.Messages;
using AlyxLibInstallerShared.Messaging.Payloads;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Source2HelperLibrary;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

///TODO: Move individual responsibilities to new services, e.g. DownloadAlyxLibService, AddonManagementService

namespace AlyxLibInstallerShared.ViewModels;
public partial class AlyxLibInstallerSharedViewModel : ObservableRecipient
{
    private readonly IDialogService _dialogService;
    private readonly ILogger? _logger;
    private FolderDeletionMonitor? _alyxLibMonitor;
    private FolderDeletionMonitor? _addonMonitor;
    private void Log(string message, LogType type = LogType.Basic, LogSeverity severity = LogSeverity.Normal) => _logger?.Log(message, type, severity);
    private void LogException(Exception ex, bool quiet = false)
    {
        if (!quiet)
            _logger?.LogError($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}");

        FileLogger.Log(ex);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileRemovalCount))]
    public partial FileGlobCollection FileRemovalGlobCollection { get; set; }

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

        if (response.Result == DialogResult.Primary)
        {
            FileRemovalGlobCollection = globClone;
            // Also edit addon config on disk
            var diskConfig = AlyxLibHelpers.GetAddonConfig(SelectedAddon);
            diskConfig.FileRemovalGlobs = [.. FileRemovalGlobCollection.Select(x => x.Name)];
            AlyxLibInstance.FileManager.SaveAddonConfig(SelectedAddon, diskConfig);

            _logger?.LogDetail("File removal list updated");
        }
        else
        {
            _logger?.LogDetail("File removal editor cancelled");
        }
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
            Message = "These files will be removed from the 'game' directory of this addon when clicking 'Remove For Upload'",
            CancelButtonText = "Close",
        }, list);
    }

    public int FileRemovalCount => SelectedAddon == null ? 0 : FileRemovalGlobCollection.GetMatchingFileCount(SelectedAddon.GamePath);

    public string WindowTitle => SelectedAddon != null ? $"AlyxLib Installer - {SelectedAddon.Name}" : "AlyxLib Installer";

    [RelayCommand]
    private void ShowAboutPopup()
    {
        var info = AboutInfoProvider.Generate();
        //var about = $"{info.AppName} v{info.Version}\n\nBy {info.Author}\n\n{info.Description}\n\nRepo: {info.Website}\n\nLicense: {info.License}";
        //_dialogService.ShowTextPopup(new DialogConfiguration { Title = "About", Message = about });
        _dialogService.ShowAboutPopup(new DialogConfiguration { Title = "About" }, info);
    }

    [ObservableProperty]
    public partial IList<SelectableAppTheme> SelectableThemes { get; private set; } = null!;


    [ObservableProperty]
    private AppTheme currentTheme;

    public event EventHandler<AppTheme>? OnThemeChanged;

    private bool _isUpdatingTheme = false;

    private void Theme_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdatingTheme) return;

        if (e.PropertyName == nameof(SelectableAppTheme.IsSelected)
            && sender is SelectableAppTheme selectedTheme
            && selectedTheme.IsSelected)
        {
            _isUpdatingTheme = true;

            foreach (var theme in SelectableThemes)
            {
                if (theme != selectedTheme)
                    theme.IsSelected = false;
            }

            CurrentTheme = selectedTheme.Theme;

            _isUpdatingTheme = false;
        }
    }

    partial void OnCurrentThemeChanged(AppTheme value)
    {
        Settings.Theme = value;
        OnThemeChanged?.Invoke(this, value);
        if (_isUpdatingTheme) return;

        _isUpdatingTheme = true;

        // Update the selection to match
        foreach (var theme in SelectableThemes)
        {
            theme.IsSelected = (theme.Theme == value);
        }

        _isUpdatingTheme = false;
    }

    void InitializeThemes()
    {
        SelectableThemes = Enum.GetValues<AppTheme>()
            .Select(x => new SelectableAppTheme(x))
            .ToList();

        foreach (var theme in SelectableThemes)
        {
            theme.PropertyChanged += Theme_PropertyChanged;
        }

        //if (SelectableThemes.Count > 0)
        //    CurrentTheme = SelectableThemes.First().Theme;
        CurrentTheme = Settings.Theme;
        OnCurrentThemeChanged(CurrentTheme);
    }

    public AlyxLibManager AlyxLibInstance { get; } = new AlyxLibManager();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstallationReady))]
    [NotifyPropertyChangedFor(nameof(ShowFolderNameWarning))]
    [NotifyPropertyChangedFor(nameof(IsAddonSelected))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial LocalAddon? SelectedAddon { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstallationReady))]
    [NotifyPropertyChangedFor(nameof(ShowFolderNameWarning))]
    [NotifyPropertyChangedFor(nameof(IsFolderNameInvalid))]
    [NotifyPropertyChangedFor(nameof(DefaultModFolderName))]
    public partial ObservableAddonConfig? AddonConfig { get; set; }

    [ObservableProperty]
    public partial bool SelectedAddonConfigFound { get; private set; } = false;

    partial void OnAddonConfigChanging(ObservableAddonConfig? value)
    {
        if (value != null)
            value.PropertyChanged -= OnAddonConfigPropertyChanged;
    }
    partial void OnAddonConfigChanged(ObservableAddonConfig? value)
    {
        if (value != null)
            value.PropertyChanged += OnAddonConfigPropertyChanged;

        OnPropertyChanged(nameof(IsInstallationReady));
    }
    private void OnAddonConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //if (e.PropertyName == nameof(ObservableAddonConfig.ModFolderName))
        //    OnPropertyChanged(nameof(IsFolderNameInvalid));
        if (e.PropertyName == nameof(ObservableAddonConfig.IsFolderNameValid))
            OnPropertyChanged(nameof(IsFolderNameInvalid));
        OnPropertyChanged(nameof(IsInstallationReady));
    }

    [ObservableProperty]
    public partial IList<SelectableLocalAddon> LocalAddons { get; private set; } = [];

    void RefreshAddons()
    {
        LocalAddons = HLA.GetAddons().Select(x => new SelectableLocalAddon(x, SelectedAddon == x)).ToList();
    }

    //public IRelayCommand SelectAddonCommand => new RelayCommand<SelectableLocalAddon>(x => SetSelectedAddon(x?.LocalAddon));
    [RelayCommand]
    public void SelectAddon(SelectableLocalAddon addon)
    {
        SelectAddon(addon.LocalAddon);
    }

    public void SelectAddon(string name)
    {
        if (!HLA.TryGetAddon(name, out LocalAddon addon))
        {
            _logger?.LogError($"Failed to find addon {name}");
            return;
        }

        if (addon == null) return;

        SelectAddon(addon);
    }

    private void ReloadAddonConfig()
    {
        if (SelectedAddon == null)
        {
            _logger?.LogError("No addon selected, cannot reload config");
            return;
        }

        SelectedAddonConfigFound = AlyxLibHelpers.AddonHasConfig(SelectedAddon);
        AddonConfig = new ObservableAddonConfig(AlyxLibHelpers.GetAddonConfig(SelectedAddon));
        FileRemovalGlobCollection = new FileGlobCollection(AddonConfig.FileRemovalGlobs);
    }

    public void SelectAddon(LocalAddon addon)
    {
        if (addon == null) return;

        // update selected state so menu item can't get unchecked
        foreach (var _addon in LocalAddons)
            _addon.IsSelected = (_addon.LocalAddon == addon);

        if (addon == SelectedAddon) return;

        _addonMonitor?.Dispose();
        _addonMonitor = new FolderDeletionMonitor(addon.ContentPath,
            onDeleted: () =>
            {
                OnPropertyChanged(nameof(IsAddonSelected));
                OnPropertyChanged(nameof(IsInstallationReady));
                _logger?.LogWarning($"Addon {addon.Name} was deleted.");
            },
            onCreated: () =>
             {
                 OnPropertyChanged(nameof(IsAddonSelected));
                 OnPropertyChanged(nameof(IsInstallationReady));
                 _logger?.LogInfo($"Addon {addon.Name} was restored.");
             });

        SelectedAddon = addon;

        if (SelectedAddon == null)
            throw new Exception("Failed to select addon, addon was null.");

        ReloadAddonConfig();

        var addonHasAlyxLib = AlyxLibHelpers.AddonHasAlyxLib(SelectedAddon);

        if (Settings.RememberLastAddon)
            Settings.LastAddon = addon.Name;

        if (SelectedAddonConfigFound)
            _logger?.LogDetail($"AlyxLib config file found for {addon.Name}");
        else if (addonHasAlyxLib)
            _logger?.LogDetail($"AlyxLib config file wasn't found for {addon.Name}, but AlyxLib was detected in addon");
        else
            _logger?.LogWarning($"AlyxLib config file wasn't found for {addon.Name}", LogSeverity.Low);

        _logger?.LogInfo($"Addon selected: {addon.Name}");

        var payload = new AddonChangedPayload(
                SelectedAddon,
                AddonConfig?.GetConfig(),
                !SelectedAddonConfigFound,
                addonHasAlyxLib
                );

        if (!SelectedAddonConfigFound && addonHasAlyxLib)
        {
            _dialogService.ShowWarningPopup(
                "Missing AlyxLib config file",
                $"It looks like {SelectedAddon.Name} has a version of AlyxLib that wasn't installed using this installer or the config file was deleted. Some options may appear incorrectly. It is recommended to backup your project before installing in case any custom AlyxLib files are modified."
                );
        }
        WeakReferenceMessenger.Default.Send(new AddonChangedMessage(payload));
        //Messenger.Send(new AddonChangedMessage(payload)); //TODO Make sure this works
    }

    [RelayCommand]
    private void PickAddon(object? ownerWindow = null)
    {
        var folder = _dialogService.PickFolder("Pick addon folder", HLA.GetAddonContentFolder());

        if (folder == null)
        {
            _logger?.LogDetail("User cancelled addon folder selection.");
            return;
        }

        var name = Path.GetFileName(folder);
        if (name == null)
        {
            _logger?.LogError($"Path is invalid: {folder}");
            return;
        }

        SelectAddon(new SelectableLocalAddon(new LocalAddon(name), false));

    }

    private static string GetUniqueAddonFolderName(string rootPath, string baseName)
    {
        var path = Path.Combine(rootPath, baseName);

        if (!Directory.Exists(path))
            return baseName;

        for (int i = 1; ; i++)
        {
            var candidateName = $"{baseName}_{i}";
            var candidatePath = Path.Combine(rootPath, candidateName);
            if (!Directory.Exists(candidatePath))
                return candidateName;
        }
    }

    public static bool IsValidAddonFolderName(string? name)
    {
        return name != null && AlyxLibHelpers.StringIsValidModName(name) && !Directory.Exists(Path.Combine(HLA.GetAddonContentFolder(), name));
    }

    [RelayCommand]
    private async Task NewAddon()
    {
        string defaultAvailableAddonName = GetUniqueAddonFolderName(HLA.GetAddonContentFolder(), "my_alyxlib_addon");
        DialogResponse result = await _dialogService.ShowTextPopup(new()
        {
            Title = "Creating New Addon",
            Message = "Choose the name of your new addon.",
            PrimaryButtonText = "Create",
            CancelButtonText = "Cancel",
            HasTextBox = true,
            TextBoxPlaceholderText = defaultAvailableAddonName,
            TextBoxValidator = IsValidAddonFolderName,
            TextBoxInvalidMessage = "Invalid addon name. Use only lowercase letters, numbers, or underscores, and make sure the name isn’t already taken.",
            Width = 250
        });

        if (result.Result != DialogResult.Primary)
        {
            _logger?.LogDetail("Cancelled creating new addon");
            return;
        }

        if (result.InputText != null)
        {
            var addonName = string.IsNullOrWhiteSpace(result.InputText) ? defaultAvailableAddonName : result.InputText;

            if (string.IsNullOrEmpty(addonName) || !AlyxLibHelpers.StringIsValidModName(addonName))
            {
                _logger?.LogError($"'{addonName}' is not a valid addon name!");
                return;
            }

            var contentPath = HLA.GetAddonContentFolder();
            if (Directory.Exists(contentPath))
            {
                Directory.CreateDirectory(Path.Combine(contentPath, addonName));
                _logger?.LogDetail("Created content folder.");
            }
            else
            {
                _logger?.LogWarning(Settings.VerboseConsole ? $"Could not find content path: '{contentPath}'" : "Could not find content path!");
                _logger?.LogWarning($"Addon '{addonName}' was not created");
                return;
            }

            var gamePath = HLA.GetAddonGameFolder();
            if (Directory.Exists(gamePath))
            {
                Directory.CreateDirectory(Path.Combine(gamePath, addonName));
                _logger?.LogDetail("Created game folder");
            }
            else
            {
                _logger?.LogWarning(Settings.VerboseConsole ? $"Could not find game path: '{gamePath}'" : "Could not find game path!");
            }

            _logger?.Log($"Successfully created addon '{addonName}'!", LogType.Success);
            SelectAddon(addonName);
        }
    }

    [RelayCommand]
    private void PickAlyxLibPath(object? ownerWindow = null)
    {
        var folder = _dialogService.PickFolder("Select AlyxLib folder", HLA.GetAddonContentFolder());

        if (folder == null)
        {
            _logger?.LogDetail("User cancelled AlyxLib folder selection. Download aborted.");
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

    void InitializeAddons()
    {
        var contentFolderWatcher = new FileSystemWatcher(HLA.GetAddonContentFolder())
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        contentFolderWatcher.Created += (_, _) => RefreshAddons();
        contentFolderWatcher.Deleted += (_, _) => RefreshAddons();

        RefreshAddons();

        if (Settings.RememberLastAddon)
        {
            if (HLA.TryGetAddon(Settings.LastAddon, out LocalAddon addon))
                SelectAddon(HLA.GetAddon(Settings.LastAddon));
            else
                _logger?.LogWarning($"Could not find addon '{Settings.LastAddon}'");
        }
    }

    public bool IsFolderNameInvalid =>
        AddonConfig == null
        || (!string.IsNullOrEmpty(AddonConfig.ModFolderName) && !AddonConfig.IsFolderNameValid)
        || (string.IsNullOrWhiteSpace(AddonConfig?.ModFolderName) && string.IsNullOrEmpty(DefaultModFolderName));

    public bool ShowFolderNameWarning => IsFolderNameInvalid && SelectedAddon != null;

    public string DefaultModFolderName =>
        !string.IsNullOrWhiteSpace(AddonConfig?.ModFolderName)
            ? AddonConfig.ModFolderName
            : SelectedAddon?.Name ?? string.Empty;

    public bool IsAddonSelected => SelectedAddon != null && Directory.Exists(SelectedAddon.ContentPath);

    public bool AlyxLibExists => AlyxLibInstance.AlyxLibExists;

    public bool IsInstallationReady =>
        AlyxLibInstance.AlyxLibExists
        && IsAddonSelected
        && SelectedAddon != null
        && AddonConfig != null
        && !IsFolderNameInvalid;

    [ObservableProperty]
    public partial bool UserHasSymlinkPrivileges { get; set; } = false;

    public Settings Settings { get; }

    public List<Tuple<string, ScriptEditor>> ScriptEditorOptions { get; } =
    [
        new Tuple<string, ScriptEditor>("None", ScriptEditor.None),
        new Tuple<string, ScriptEditor>("VS Code", ScriptEditor.VisualStudioCode),
    ];

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

        FileRemovalGlobCollection = new FileGlobCollection();
        //FileRemovalGlobCollection.CollectionChanged += (_, _) => FileRemovalCount = FileRemovalGlobCollection.GetMatchingFileCount(SelectedAddon?.GamePath);
        FileRemovalGlobCollection.CollectionChanged += (_, _) =>
        {
            if (AddonConfig == null)
            {
                _logger?.LogError("Trying to update file removal list when no addon is selected!");
                return;
            }

            //List<string> newGlobs = [];
            //foreach (EditableEntry glob in FileRemovalGlobCollection)
            //    newGlobs.Add(glob);

            //AddonConfig.FileRemovalGlobs = newGlobs;

            AddonConfig.FileRemovalGlobs = [.. FileRemovalGlobCollection.Select(x => x.Name)];
            OnPropertyChanged(nameof(FileRemovalCount));
        };

    }

    [ObservableProperty]
    public partial bool IsInitializing { get; set; } = true;

    public async void InitializeAsync()
    {
        IsInitializing = true;

        InitializeThemes();

        SettingsManager.Save();

        // idk what this does yet
        IsActive = true;

        _logger?.Log($"Settings file: {SettingsManager.Path}");
        _logger?.Log($"Log file: {FileLogger.LogFilePath}");

        // Check for log file accumulation and notify user in the debug console if needed
        var logFileCount = FileLogger.LogFileCount;
        if (logFileCount > Settings.MaxLogFiles)
        {
            if (Settings.AutoDeleteLogFiles)
            {
                var oldFileCount = logFileCount - Settings.MaxLogFiles;
                _logger?.LogDetail($"Deleting {oldFileCount} old log file{(oldFileCount == 1 ? "" : "s")}...");
                FileLogger.CleanupOldLogs(Settings.MaxLogFiles);
            }
            else
            {
                // Compose a warning/info message with a clickable logs folder path
                string logsPath = Path.GetDirectoryName(FileLogger.LogFilePath) ?? "";
                string msg =
                    $"There are currently {logFileCount} log files in your logs folder.\n" +
                    $"You may want to clean them up to save disk space.\n" +
                    $"Logs folder: {logsPath}";

                _logger?.LogWarning(msg);
            }
        }

        InitializeAddons();

        if (string.IsNullOrEmpty(Settings.AlyxLibDirectory) || !TrySetAlyxLibPath(Settings.AlyxLibDirectory))
        {
            //var response = await _dialogService.ShowIntroAlyxLibPopup();
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
                    DownloadAlyxLib();
                    break;
                case DialogResult.Secondary:
                    PickAlyxLibPath();
                    break;
            }
        }

        UserHasSymlinkPrivileges = PrivilegeChecker.CanCreateSymlinks();
        if (!UserHasSymlinkPrivileges)
        {
            //_dialogService.ShowWarningPopup("This installer requires developer mode or administrator privileges to install.");
            await _dialogService.ShowPrivilegeWarning();
        }

        if (AlyxLibInstance.AlyxLibExists)
        {
            if (!Settings.GetDontShowAgain("StartupUpdate"))
            {
                var result = await GetAlyxLibVersionComparison();

                if (result.RemoteConnectionFailed)
                {
                    _logger?.LogWarning("Unable to check for updates. Please check your internet connection.", LogSeverity.Low);
                    goto SkipUpdateCheck;
                }

                if (result.RemoteIsNewer)
                {
                    //_dialogService.ShowWarningPopup($"New version available!\nv{result.LocalVersion} -> v{result.RemoteVersion}\nUse Help -> Check for Updates to download.");
                    var response = await _dialogService.ShowTextPopup(new DialogConfiguration
                    {
                        Title = "New version available!",
                        Message = $"Local version: v{result.LocalVersion}\nRemote version: v{result.RemoteVersion}\n\nWould you like to download the latest version?",
                        PrimaryButtonText = "Download",
                        CancelButtonText = "Cancel",

                        HasCheckBox = true,
                        CheckBoxDefaultChecked = Settings.GetDontShowAgain("StartupUpdate"),
                    });

                    if (response.CheckboxChecked == true)
                    {
                        Settings.SetDontShowAgain("StartupUpdate", true);
                    }

                    switch (response.Result)
                    {
                        case DialogResult.Primary:
                            DownloadAlyxLib();
                            break;
                    }
                }
            }
        SkipUpdateCheck:;
        }

        IsInitializing = false;
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        try
        {
            _logger?.Log("Checking for updates...");

            SetLoading(true, "Checking for updates");

            var result = await GetAlyxLibVersionComparison();

            if (result.RemoteConnectionFailed)
            {
                _logger?.LogWarning("Unable to check for updates. Please check your internet connection.", LogSeverity.Low);
                await _dialogService.ShowWarningPopup(new DialogConfiguration()
                {
                    Title = "Connection Failed",
                    Message = "Unable to check for updates. Please check your internet connection."
                });
                return;
            }

            _logger?.LogDetail($"Version diff: {result.Comparison}");

            if (result.RemoteIsNewer)
            {
                _logger?.LogInfo($"Update available: {result.LocalVersion} -> {result.RemoteVersion}");

                var response = await _dialogService.ShowTextPopup(new()
                {
                    Title = "Update Available",
                    Message = $"New version available!\nv{result.LocalVersion} -> v{result.RemoteVersion}",
                    PrimaryButtonText = "Download",
                    CancelButtonText = "Cancel",
                });

                if (response.Result == DialogResult.Primary)
                {
                    DownloadAlyxLib();
                    return;
                }

                _logger?.LogDetail("Update cancelled.");
            }
            else if (result.SameVersion)
            {
                _logger?.LogInfo("AlyxLib is up to date.");
                await _dialogService.ShowTextPopup(new DialogConfiguration()
                {
                    Title = "Up to date",
                    Message = $"You are up to date!\nLocal: v{result.LocalVersion}\nRemote: v{result.RemoteVersion}"
                });
            }
            else
            {
                _logger?.LogInfo("Local AlyxLib version is ahead of remote");
                await _dialogService.ShowTextPopup(new DialogConfiguration()
                {
                    Title = "Ahead of latest version",
                    Message = "You are ahead of the latest version somehow, you must have a pre-release version."
                });
            }
        }
        catch (Exception ex)
        {
            LogException(ex);
        }
        finally
        {
            SetLoading(false);
        }
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

    [SupportedOSPlatform("windows")]
    [RelayCommand]
    private void RegisterExplorerContextMenu()
    {
        try
        {
            ShellContextMenuRegistrar.RegisterExplorerContextMenu("Open with AlyxLibInstaller");

            var message = Utils.IsWindows11OrGreater()
                ? "Shift+Right Click any folder or blank space in a folder to use AlyxLibInstaller."
                : "Right Click any folder or blank space in a folder to use AlyxLibInstaller.";

            _dialogService.ShowTextPopup(new DialogConfiguration()
            {
                Title = "Success",
                Message = message
            });

            _logger?.LogInfo("Context menu added to registry.", LogSeverity.Low);
        }
        catch (Exception ex)
        {
            FileLogger.Log(ex, "Failed to add context menu");
            _dialogService.ShowWarningPopup($"Failed to add context menu:\n{ex.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    [RelayCommand]
    private void UnregisterExplorerContextMenu()
    {
        try
        {
            ShellContextMenuRegistrar.UnregisterExplorerContextMenu();

            _dialogService.ShowTextPopup(new DialogConfiguration()
            {
                Title = "Success",
                Message = "Context menu removed."
            });

            _logger?.LogInfo("Context menu removed from registry", LogSeverity.Low);
        }
        catch (Exception ex)
        {
            FileLogger.Log(ex, $"Failed to remove context menu");
            _dialogService.ShowWarningPopup($"Failed to remove context menu:\n{ex.Message}");
        }
    }

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

    private void OpenAddonPath(string? path, string name = "addon")
    {
        if (path == null)
        {
            _logger?.LogError($"No addon selected, cannot open {name} folder");
            return;
        }

        if (!Directory.Exists(path))
        {
            _logger?.LogError($"Could not find {name} folder: {path}");
            _dialogService.ShowWarningPopup($"Could not find {name} folder: {path}");
            return;
        }

        _logger?.LogDetail($"Opening {name} folder: {path}");
        Launcher.Launch(path);
    }

    [RelayCommand]
    private void OpenAddonContentFolder() => OpenAddonPath(SelectedAddon?.ContentPath, "content");

    [RelayCommand]
    private void OpenAddonGameFolder() => OpenAddonPath(SelectedAddon?.GamePath, "game");

    [RelayCommand]
    private void InstallAlyxLib()
    {
        //File.Create(Path.Combine(AlyxLib.AlyxLibPath.FullName, "testfile.txt"));
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot install AlyxLib");
            return;
        }

        if (IsFolderNameInvalid)
        {
            _logger?.LogError("Invalid mod folder name, cannot install AlyxLib");
            return;
        }

        SetLoading(true, "Installing AlyxLib");

        AlyxLibInstance.FileManager.InstallAlyxLib(SelectedAddon, new()
        {
            VScriptInstalled = AddonConfig.VScriptInstalled,
            SoundEventInstalled = AddonConfig.SoundEventInstalled,
            PanoramaInstalled = AddonConfig.PanoramaInstalled,
            GitInstalled = AddonConfig.GitInstalled,
            ModFolderName = DefaultModFolderName,
            EditorType = AddonConfig.EditorType,

            FileRemovalGlobs = AddonConfig.FileRemovalGlobs,

            Version = AlyxLibInstance.AlyxLibVersion
        });

        ReloadAddonConfig();

        SetLoading(false);
        AlyxLibRemovedFromAddonForUpload = false;
    }

    [ObservableProperty]
    public partial bool AlyxLibRemovedFromAddonForUpload { get; set; } = false;

    [RelayCommand]
    private void RemoveAlyxLibFromAddonForUpload()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot remove AlyxLib for upload");
            return;
        }

        SetLoading(true, "Removing AlyxLib...");

        AlyxLibInstance.FileManager.UninstallAlyxLibForUpload(SelectedAddon, AddonConfig.GetConfig());

        SetLoading(false);

        AlyxLibRemovedFromAddonForUpload = true;
    }

    [RelayCommand]
    private async Task UninstallAlyxLib()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot uninstall AlyxLib");
            return;
        }

        // Check if options are different from currently installed (same options = no need to uninstall)
        var installedConfig = AlyxLibHelpers.GetAddonConfig(SelectedAddon);
        if (installedConfig.VScriptInstalled == AddonConfig.VScriptInstalled &&
            installedConfig.SoundEventInstalled == AddonConfig.SoundEventInstalled &&
            installedConfig.PanoramaInstalled == AddonConfig.PanoramaInstalled &&
            installedConfig.GitInstalled == AddonConfig.GitInstalled)
        {
            _logger?.LogInfo("Options match currently installed, skipping uninstall...");
            return;
        }


        // Warn user about removal of git repository
        //if (AddonConfig.GitInstalled == false && Repository.IsValid(SelectedAddon.ContentPath))
        if (AddonConfig.GitInstalled == false && GitHelper.FolderHasGitRepository(SelectedAddon.ContentPath))
        {
            var response = await _dialogService.ShowTextPopup(new DialogConfiguration()
            {
                Title = "Delete Git Repository?",
                Message = "A Git repository exists in this addon folder and you have unselected the Git option.\n\n" +
                "Are you sure you would like to continue and delete the Git repository?",
                PrimaryButtonText = "Yes, delete it",
                CancelButtonText = "No, go back"
            });

            switch (response.Result)
            {
                case DialogResult.Primary:
                    _logger?.LogDetail("User cancelled Git removal");
                    return;
            }
        }

        // Create display string of parts to uninstall for user
        var parts = new List<string>();
        if (!AddonConfig.VScriptInstalled) parts.Add("VScript");
        if (!AddonConfig.SoundEventInstalled) parts.Add("Sounds");
        if (!AddonConfig.PanoramaInstalled) parts.Add("Panorama");
        if (!AddonConfig.GitInstalled) parts.Add("Git");
        string desc = string.Join(", ", parts);

        _logger?.LogInfo($"Removing {desc} ...");

        AlyxLibInstance.FileManager.UninstallAlyxLibOptions(SelectedAddon, AddonConfig.GetConfig());
    }

    [RelayCommand]
    private async Task RemoveAllAlyxLibFiles()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot uninstall AlyxLib");
            return;
        }

        var response = await _dialogService.ShowTextPopup(new DialogConfiguration()
        {
            Title = "Delete all AlyxLib files?",
            Message = "Are you sure you would like to delete all AlyxLib files?",
            PrimaryButtonText = "Yes, delete them",
            CancelButtonText = "No, go back"
        });

        switch (response.Result)
        {
            case DialogResult.Primary:
                //AlyxLibInstance.FileManager.UninstallAlyxLibOptions(SelectedAddon, new()
                //{
                //    ModFolderName = AddonConfig.ModFolderName,
                //    Version = AddonConfig.Version,
                //    EditorType = AddonConfig.EditorType
                //});
                AlyxLibInstance.FileManager.UninstallAlyxLibFully(SelectedAddon);
                ReloadAddonConfig();
                break;
            default:
                _logger?.LogDetail("User cancelled AlyxLib removal");
                break;
        }
    }

    [RelayCommand]
    private void OpenAlyxLibWiki() => Launcher.Launch("https://github.com/FrostSource/alyxlib/wiki");
    [RelayCommand]
    private void OpenAlyxLibRepo() => Launcher.Launch("https://github.com/FrostSource/alyxlib");

    /// <summary>
    /// Indicates whether the installer is working on something and the UI should be blocked.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCurrentlyLoading { get; set; } = false;
    [ObservableProperty]
    public partial string LoadingOverlayText { get; set; } = "";
    private void SetLoading(bool isLoading, string text = "") => (IsCurrentlyLoading, LoadingOverlayText) = (isLoading, text);

    [RelayCommand]
    private void DownloadAlyxLib() => DownloadAlyxLib(true);

    private async void DownloadAlyxLib(bool newLocation = false)
    {
        string downloadPath;

        if (!AlyxLibInstance.AlyxLibExists || newLocation)
        {
            var folder = _dialogService.PickFolder();
            if (folder == null)
            {
                _logger?.LogDetail("User did not select a folder, download cancelled.");
                return;
            }

            downloadPath = folder;
            var extractPathInfo = new PathInfo(downloadPath);
            if (extractPathInfo.Name != "alyxlib")
            {
                downloadPath = Path.Combine(downloadPath, "alyxlib");
            }
        }
        else
        {
            downloadPath = AlyxLibInstance.AlyxLibPath.FullName;
        }

        SetLoading(true, "Downloading AlyxLib...");

        _logger?.LogDetail("Downloading AlyxLib...");

        try
        {
            await AlyxLibHelpers.DownloadRepository(Settings.GitHubUrl, downloadPath);
        }
        catch (HttpRequestException ex)
        {
            //_logger?.LogError($"Failed to download AlyxLib. Please check your internet connection.");
            LogException(ex, true);
            await _dialogService.ShowWarningPopup(new DialogConfiguration()
            {
                Title = "Connection Failed",
                Message = "Unable to download AlyxLib. Please check your internet connection."
            });
            return;
        }
        finally
        {
            SetLoading(false);
        }

        if (TrySetAlyxLibPath(downloadPath))
            _logger?.Log($"AlyxLib path set to '{downloadPath}'", LogType.Success);
        else
            _logger?.LogError($"Failed to set AlyxLib path to '{downloadPath}'");

        if (AlyxLibInstance.VersionManager.TryGetLocalVersion(out string localVersion))
            _logger?.Log($"AlyxLib v{localVersion} downloaded successfully!", LogType.Success);
        else
            _logger?.Log($"AlyxLib downloaded successfully!", LogType.Success);
    }

    public async Task<VersionComparisonResult> GetAlyxLibVersionComparison()
    {
        try
        {
            return await AlyxLibInstance.VersionManager.CompareVersions(Settings.GitHubVersionFileUrl);
        }
        catch (Exception ex)
        {
            LogException(ex);
            return VersionComparisonResult.Failed;
        }
    }
}
