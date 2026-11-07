using AlyxLib;
using AlyxLib.Logging;
using AlyxLibInstallerShared.Services.Dialog;

namespace AlyxLibInstallerShared.Services;

/// <summary>
/// Downloads AlyxLib and checks for updates.
/// </summary>
/// <param name="trySetAlyxLibPath">Sets the AlyxLib folder used by the installer, returning false if it isn't a valid AlyxLib folder.</param>
/// <param name="setLoading">Shows or hides the loading overlay with a message.</param>
public class AlyxLibUpdateService(
    IDialogService dialogService,
    AlyxLibManager manager,
    Settings settings,
    Func<string, bool> trySetAlyxLibPath,
    Action<bool, string> setLoading,
    ILogger? logger = null,
    Action<Exception>? logException = null)
{
    private const string StartupUpdateKey = "StartupUpdate";

    private readonly Action<Exception> _logException = logException ?? FileLogger.Log;

    /// <summary>
    /// Downloads the AlyxLib repository to a folder. Replaceable for testing.
    /// </summary>
    public Func<string, Task>? DownloadRepository { get; init; }

    /// <summary>
    /// Compares the local AlyxLib version with the latest version. Replaceable for testing.
    /// </summary>
    public Func<Task<VersionComparisonResult>>? CompareVersions { get; init; }

    private Func<string, Task> Download => DownloadRepository
        ?? (path => AlyxLibHelpers.DownloadRepository(settings.GitHubUrl, path, logger: logger));

    private Func<Task<VersionComparisonResult>> Compare => CompareVersions
        ?? (() => manager.VersionManager.CompareVersions(settings.GitHubVersionFileUrl));

    /// <summary>
    /// Gets the version comparison, returning <see cref="VersionComparisonResult.Failed"/> instead of throwing.
    /// </summary>
    public async Task<VersionComparisonResult> GetVersionComparison()
    {
        try
        {
            return await Compare();
        }
        catch (Exception ex)
        {
            logger?.LogError($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}");
            _logException(ex);
            return VersionComparisonResult.Failed;
        }
    }

    /// <summary>
    /// Updates the current AlyxLib folder in place so addons already linked to it receive the update.
    /// </summary>
    public async Task Update()
    {
        if (manager.AlyxLibExists && GitHelper.FolderIsRepositoryRoot(manager.AlyxLibPath.FullName))
        {
            // Extracting over a clone would overwrite any local changes
            var response = await dialogService.ShowWarningPopup(new DialogConfiguration()
            {
                Title = "AlyxLib is a Git repository",
                Message = $"Your AlyxLib folder is a Git repository:\n{manager.AlyxLibPath.FullName}\n\n" +
                "Pull the latest changes with Git instead, so your local changes aren't overwritten.",
                PrimaryButtonText = "Download to a new folder",
                CancelButtonText = "Cancel"
            });

            if (response.Result == DialogResult.Primary)
                await DownloadAlyxLib(newLocation: true);
            else
                logger?.LogDetail("Update cancelled, AlyxLib folder is a Git repository.");

            return;
        }

        await DownloadAlyxLib(newLocation: false);
    }

    /// <summary>
    /// Downloads AlyxLib, asking for a folder if <paramref name="newLocation"/> is true or there is no current AlyxLib folder.
    /// </summary>
    public async Task DownloadAlyxLib(bool newLocation = false)
    {
        string downloadPath;

        if (!manager.AlyxLibExists || newLocation)
        {
            var folder = dialogService.PickFolder();
            if (folder == null)
            {
                logger?.LogDetail("User did not select a folder, download cancelled.");
                return;
            }

            downloadPath = folder;
            if (new PathInfo(downloadPath).Name != "alyxlib")
                downloadPath = Path.Combine(downloadPath, "alyxlib");
        }
        else
        {
            downloadPath = manager.AlyxLibPath.FullName;
        }

        setLoading(true, "Downloading AlyxLib...");

        logger?.LogDetail("Downloading AlyxLib...");

        try
        {
            await Download(downloadPath);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // TaskCanceledException is thrown when the download times out
            _logException(ex);
            await dialogService.ShowWarningPopup(new DialogConfiguration()
            {
                Title = "Connection Failed",
                Message = "Unable to download AlyxLib. Please check your internet connection."
            });
            return;
        }
        catch (Exception ex)
        {
            _logException(ex);
            await dialogService.ShowWarningPopup(new DialogConfiguration()
            {
                Title = "Download Failed",
                Message = $"Unable to download or extract AlyxLib to '{downloadPath}'.\n\n{ex.Message}"
            });
            return;
        }
        finally
        {
            setLoading(false, "");
        }

        if (trySetAlyxLibPath(downloadPath))
            logger?.Log($"AlyxLib path set to '{downloadPath}'", LogType.Success);
        else
            logger?.LogError($"Failed to set AlyxLib path to '{downloadPath}'");

        if (manager.VersionManager.TryGetLocalVersion(out string localVersion))
            logger?.Log($"AlyxLib v{localVersion} downloaded successfully!", LogType.Success);
        else
            logger?.Log($"AlyxLib downloaded successfully!", LogType.Success);
    }

    /// <summary>
    /// Checks for updates when the user asks, showing the result.
    /// </summary>
    public async Task CheckForUpdates()
    {
        try
        {
            logger?.Log("Checking for updates...");

            setLoading(true, "Checking for updates");

            var result = await GetVersionComparison();

            if (result.RemoteConnectionFailed)
            {
                logger?.LogWarning("Unable to check for updates. Please check your internet connection.", LogSeverity.Low);
                await dialogService.ShowWarningPopup(new DialogConfiguration()
                {
                    Title = "Connection Failed",
                    Message = "Unable to check for updates. Please check your internet connection."
                });
                return;
            }

            logger?.LogDetail($"Version diff: {result.Comparison}");

            if (result.RemoteIsNewer)
            {
                logger?.LogInfo($"Update available: {result.LocalVersion} -> {result.RemoteVersion}");

                var response = await dialogService.ShowTextPopup(new()
                {
                    Title = "Update Available",
                    Message = $"New version available!\nv{result.LocalVersion} -> v{result.RemoteVersion}",
                    PrimaryButtonText = "Download",
                    CancelButtonText = "Cancel",
                });

                if (response.Result == DialogResult.Primary)
                {
                    await Update();
                    return;
                }

                logger?.LogDetail("Update cancelled.");
            }
            else if (result.SameVersion)
            {
                logger?.LogInfo("AlyxLib is up to date.");
                await dialogService.ShowTextPopup(new DialogConfiguration()
                {
                    Title = "Up to date",
                    Message = $"You are up to date!\nLocal: v{result.LocalVersion}\nRemote: v{result.RemoteVersion}"
                });
            }
            else
            {
                logger?.LogInfo("Local AlyxLib version is ahead of remote");
                await dialogService.ShowTextPopup(new DialogConfiguration()
                {
                    Title = "Ahead of latest version",
                    Message = "You are ahead of the latest version somehow, you must have a pre-release version."
                });
            }
        }
        catch (Exception ex)
        {
            logger?.LogError($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}");
            _logException(ex);
        }
        finally
        {
            setLoading(false, "");
        }
    }

    /// <summary>
    /// Checks for updates on startup, only prompting if a newer version exists and the user hasn't turned the prompt off.
    /// </summary>
    public async Task CheckForUpdatesOnStartup()
    {
        if (!manager.AlyxLibExists || settings.GetDontShowAgain(StartupUpdateKey))
            return;

        var result = await GetVersionComparison();

        if (result.RemoteConnectionFailed)
        {
            logger?.LogWarning("Unable to check for updates. Please check your internet connection.", LogSeverity.Low);
            return;
        }

        if (!result.RemoteIsNewer)
            return;

        var response = await dialogService.ShowTextPopup(new DialogConfiguration
        {
            Title = "New version available!",
            Message = $"Local version: v{result.LocalVersion}\nRemote version: v{result.RemoteVersion}\n\nWould you like to download the latest version?",
            PrimaryButtonText = "Download",
            CancelButtonText = "Cancel",

            HasCheckBox = true,
            CheckBoxDefaultChecked = settings.GetDontShowAgain(StartupUpdateKey),
        });

        settings.SetDontShowAgain(StartupUpdateKey, response.CheckboxChecked == true);

        if (response.Result == DialogResult.Primary)
            await Update();
    }
}
