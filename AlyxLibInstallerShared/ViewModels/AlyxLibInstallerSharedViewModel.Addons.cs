using AlyxLib;
using AlyxLib.Logging;
using AlyxLib.Services;
using AlyxLibInstallerShared.Messaging.Messages;
using AlyxLibInstallerShared.Messaging.Payloads;
using AlyxLibInstallerShared.Models;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Source2HelperLibrary;
using System.ComponentModel;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    private FolderDeletionMonitor? _addonMonitor;
    private FileSystemWatcher? _addonGameFolderMonitor;
    private FileSystemWatcher? _addonsFolderMonitor;

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

    [ObservableProperty]
    public partial IList<SelectableLocalAddon> LocalAddons { get; private set; } = [];

    public List<Tuple<string, ScriptEditor>> ScriptEditorOptions { get; } =
    [
        new Tuple<string, ScriptEditor>("None", ScriptEditor.None),
        new Tuple<string, ScriptEditor>("VS Code", ScriptEditor.VisualStudioCode),
    ];

    public string WindowTitle => SelectedAddon != null ? $"AlyxLib Installer - {SelectedAddon.Name}" : "AlyxLib Installer";

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

    public bool IsInstallationReady =>
        AlyxLibInstance.AlyxLibExists
        && IsAddonSelected
        && SelectedAddon != null
        && AddonConfig != null
        && !IsFolderNameInvalid;

    /// <summary>
    /// Path of an addon, or any folder inside one, to select on startup. Set when opened from the Explorer context menu.
    /// </summary>
    public string? StartupAddonPath { get; set; }

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
        if (e.PropertyName == nameof(ObservableAddonConfig.IsFolderNameValid))
            OnPropertyChanged(nameof(IsFolderNameInvalid));
        OnPropertyChanged(nameof(IsInstallationReady));
    }

    void RefreshAddons()
    {
        LocalAddons = HLA.GetAddons().Select(x => new SelectableLocalAddon(x, SelectedAddon == x)).ToList();
    }

    void InitializeAddons()
    {
        string addonsFolder;
        try
        {
            addonsFolder = HLA.GetAddonContentFolder();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Could not find the Half-Life: Alyx install folder.");
            LogException(ex, true);
            return;
        }

        // The content folder only exists once the Workshop Tools DLC is installed
        if (!Directory.Exists(addonsFolder))
        {
            _logger?.LogWarning($"Addon content folder not found: {addonsFolder}. Make sure the Half-Life: Alyx Workshop Tools are installed.");
            return;
        }

        _addonsFolderMonitor = new FileSystemWatcher(addonsFolder)
        {
            NotifyFilter = NotifyFilters.DirectoryName,
            EnableRaisingEvents = true
        };

        _addonsFolderMonitor.Created += (_, _) => RefreshAddons();
        _addonsFolderMonitor.Deleted += (_, _) => RefreshAddons();

        RefreshAddons();

        // An addon passed on the command line takes priority over the last used addon
        if (!string.IsNullOrEmpty(StartupAddonPath))
        {
            var startupAddonName = GetAddonNameFromPath(StartupAddonPath);
            if (startupAddonName != null && HLA.TryGetAddon(startupAddonName, out LocalAddon startupAddon))
            {
                SelectAddon(startupAddon);
                return;
            }

            _logger?.LogWarning($"Could not find an addon for '{StartupAddonPath}'");
        }

        if (Settings.RememberLastAddon)
        {
            if (HLA.TryGetAddon(Settings.LastAddon, out LocalAddon addon))
                SelectAddon(addon);
            else
                _logger?.LogWarning($"Could not find addon '{Settings.LastAddon}'");
        }
    }

    /// <summary>
    /// Gets the addon name from a path to an addon folder or any folder inside it,
    /// e.g. "...\content\hlvr_addons\my_addon\scripts\vscripts" gives "my_addon".
    /// </summary>
    public static string? GetAddonNameFromPath(string path)
    {
        var parts = Path.GetFullPath(path).Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        var addonsIndex = Array.FindLastIndex(parts, part => part.Equals("hlvr_addons", StringComparison.OrdinalIgnoreCase));
        if (addonsIndex >= 0)
            return addonsIndex + 1 < parts.Length ? parts[addonsIndex + 1] : null;

        // Not inside an addons folder, assume the folder itself is the addon
        return parts.Length > 0 ? parts[^1] : null;
    }

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

    public async void SelectAddon(LocalAddon addon)
    {
        try
        {
            await SelectAddonAsync(addon);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to select addon {addon?.Name}.");
            LogException(ex);
        }
    }

    private async Task SelectAddonAsync(LocalAddon addon)
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

        // Hopefully watching the game folder isn't a big overhead
        _addonGameFolderMonitor?.Dispose();
        _addonGameFolderMonitor = null;
        // A content-only addon (e.g. freshly cloned) has no game folder yet, and watching a missing folder throws
        if (Directory.Exists(addon.GamePath))
        {
            _addonGameFolderMonitor = new FileSystemWatcher(addon.GamePath)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            // Automatically update the file removal count
            _addonGameFolderMonitor.Created += (_, _) => QueueFileRemovalCountUpdate();
            _addonGameFolderMonitor.Deleted += (_, _) => QueueFileRemovalCountUpdate();
            _addonGameFolderMonitor.Renamed += (_, _) => QueueFileRemovalCountUpdate();
        }

        SelectedAddon = addon;

        if (SelectedAddon == null)
            throw new Exception("Failed to select addon, addon was null.");

        ReloadAddonConfig();

        var addonHasAlyxLib = AlyxLibHelpers.AddonHasAlyxLib(SelectedAddon);

        // Show the reinstall reminder if the addon is still in its removed for upload state, e.g. after a restart
        AlyxLibRemovedFromAddonForUpload = FileManager.HasTemporarilyMovedUploadFiles(SelectedAddon)
            || (SelectedAddonConfigFound && AddonConfig!.VScriptInstalled && !addonHasAlyxLib);

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

        if (!SelectedAddonConfigFound && addonHasAlyxLib && !Settings.GetDontShowAgain("MissingAlyxLibConfig"))
        {
            DialogResponse response = await _dialogService.ShowWarningPopup(new DialogConfiguration
            {
                Title = "Missing AlyxLib config file",
                Message = $"It looks like {SelectedAddon.Name} has a version of AlyxLib that wasn't installed using this installer or the config file was deleted. Some options may appear incorrectly.\n\nIt is recommended to backup your project before installing in case any custom AlyxLib files are modified.",
                CheckBoxDefaultChecked = Settings.GetDontShowAgain("MissingAlyxLibConfig"),
                HasCheckBox = true
            });

            Settings.SetDontShowAgain("MissingAlyxLibConfig", response.CheckboxChecked == true);
        }

        WeakReferenceMessenger.Default.Send(new AddonChangedMessage(payload));
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

        if (result.InputText == null)
            return;

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
