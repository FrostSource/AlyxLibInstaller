using AlyxLib.Logging;
using AlyxLibInstallerShared.Services.Dialog;
using CommunityToolkit.Mvvm.Input;
using System.Runtime.Versioning;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    [RelayCommand]
    private void ShowAboutPopup()
    {
        _dialogService.ShowAboutPopup(new DialogConfiguration { Title = "About" }, AboutInfoProvider.Generate());
    }

    [RelayCommand]
    private void OpenAlyxLibWiki() => Launcher.Launch("https://github.com/FrostSource/alyxlib/wiki");

    [RelayCommand]
    private void OpenAlyxLibRepo() => Launcher.Launch("https://github.com/FrostSource/alyxlib");

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
}
