using AlyxLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlyxLibInstallerShared.ViewModels;

public partial class AlyxLibInstallerSharedViewModel
{
    [ObservableProperty]
    public partial bool AlyxLibRemovedFromAddonForUpload { get; set; } = false;

    [RelayCommand]
    private void InstallAlyxLib()
    {
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

        try
        {
            var installed = _installService.Install(SelectedAddon, new()
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

            if (installed)
                AlyxLibRemovedFromAddonForUpload = false;
        }
        finally
        {
            ReloadAddonConfig();
            SetLoading(false);
        }
    }

    [RelayCommand]
    private void RemoveAlyxLibFromAddonForUpload()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot remove AlyxLib for upload");
            return;
        }

        SetLoading(true, "Removing AlyxLib...");

        try
        {
            _installService.RemoveForUpload(SelectedAddon, AddonConfig.GetConfig());
        }
        finally
        {
            SetLoading(false);
            // Some files may have been removed or moved even on failure, so the reminder to reinstall is always shown
            AlyxLibRemovedFromAddonForUpload = true;
        }
    }

    [RelayCommand]
    private async Task UninstallAlyxLib()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot uninstall AlyxLib");
            return;
        }

        if (await _installService.UninstallUntickedOptions(SelectedAddon, AddonConfig.GetConfig()))
            ReloadAddonConfig();
    }

    [RelayCommand]
    private async Task RemoveAllAlyxLibFiles()
    {
        if (SelectedAddon == null || AddonConfig == null)
        {
            _logger?.LogError("No addon selected, cannot uninstall AlyxLib");
            return;
        }

        if (await _installService.RemoveAll(SelectedAddon))
            ReloadAddonConfig();
    }
}
