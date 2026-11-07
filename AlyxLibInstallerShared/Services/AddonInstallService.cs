using AlyxLib;
using AlyxLib.Logging;
using AlyxLibInstallerShared.Services.Dialog;
using Source2HelperLibrary;

namespace AlyxLibInstallerShared.Services;

/// <summary>
/// Installs and removes AlyxLib from addons, asking the user for confirmation where needed.
/// </summary>
public class AddonInstallService(IDialogService dialogService, IAddonFileOperations files, ILogger? logger = null, Action<Exception>? logException = null)
{
    private readonly Action<Exception> _logException = logException ?? FileLogger.Log;

    /// <summary>
    /// Installs AlyxLib into the addon.
    /// </summary>
    /// <returns>False if the install threw an exception.</returns>
    public bool Install(LocalAddon addon, AddonConfig options)
    {
        try
        {
            files.Install(addon, options);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError("AlyxLib installation failed.");
            _logException(ex);
            return false;
        }
    }

    /// <summary>
    /// Removes AlyxLib files that shouldn't be uploaded to the workshop.
    /// </summary>
    public void RemoveForUpload(LocalAddon addon, AddonConfig options)
    {
        try
        {
            files.RemoveForUpload(addon, options);
        }
        catch (Exception ex)
        {
            logger?.LogError("Removing AlyxLib for upload failed.");
            _logException(ex);
        }
    }

    /// <summary>
    /// Removes the parts of AlyxLib that are unticked in <paramref name="selected"/> but currently installed.
    /// Asks before deleting the addon's Git repository.
    /// </summary>
    /// <returns>True if anything was uninstalled.</returns>
    public async Task<bool> UninstallUntickedOptions(LocalAddon addon, AddonConfig selected)
    {
        // Same options as installed means there is nothing to uninstall
        var installed = files.GetInstalledConfig(addon);
        if (installed.VScriptInstalled == selected.VScriptInstalled &&
            installed.SoundEventInstalled == selected.SoundEventInstalled &&
            installed.PanoramaInstalled == selected.PanoramaInstalled &&
            installed.GitInstalled == selected.GitInstalled)
        {
            logger?.LogInfo("Options match currently installed, skipping uninstall...");
            return false;
        }

        if (!selected.GitInstalled && files.HasGitRepository(addon))
        {
            var response = await dialogService.ShowTextPopup(new DialogConfiguration()
            {
                Title = "Delete Git Repository?",
                Message = "A Git repository exists in this addon folder and you have unselected the Git option.\n\n" +
                "Are you sure you would like to continue and delete the Git repository?",
                PrimaryButtonText = "Yes, delete it",
                CancelButtonText = "No, go back"
            });

            if (response.Result != DialogResult.Primary)
            {
                logger?.LogDetail("User cancelled Git removal");
                return false;
            }
        }

        // Create display string of parts to uninstall for user
        var parts = new List<string>();
        if (!selected.VScriptInstalled) parts.Add("VScript");
        if (!selected.SoundEventInstalled) parts.Add("Sounds");
        if (!selected.PanoramaInstalled) parts.Add("Panorama");
        if (!selected.GitInstalled) parts.Add("Git");

        logger?.LogInfo($"Removing {string.Join(", ", parts)} ...");

        files.UninstallOptions(addon, selected);
        return true;
    }

    /// <summary>
    /// Removes all AlyxLib files from the addon after confirming with the user.
    /// The Git repository is only deleted if the user ticks the checkbox for it.
    /// </summary>
    /// <returns>True if AlyxLib was removed.</returns>
    public async Task<bool> RemoveAll(LocalAddon addon)
    {
        bool hasGitRepository = files.HasGitRepository(addon);

        var response = await dialogService.ShowTextPopup(new DialogConfiguration()
        {
            Title = "Delete all AlyxLib files?",
            Message = "Are you sure you would like to delete all AlyxLib files?",
            PrimaryButtonText = "Yes, delete them",
            CancelButtonText = "No, go back",
            HasCheckBox = hasGitRepository,
            CheckBoxText = "Also delete the Git repository (.git folder and its history)",
            CheckBoxDefaultChecked = false
        });

        if (response.Result != DialogResult.Primary)
        {
            logger?.LogDetail("User cancelled AlyxLib removal");
            return false;
        }

        bool removeGitRepository = hasGitRepository && response.CheckboxChecked == true;
        files.UninstallFully(addon, removeGitRepository);
        return true;
    }
}
