using Source2HelperLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlyxLibInstaller.AlyxLib;

internal static class AlyxLibHelpers
{

    /// <summary>
    /// Checks if the addon has the main AlyxLib script folder. This is just to check if AlyxLib exists in some form.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public static bool AddonHasAlyxLib(LocalAddon addon)
    {
        return File.Exists(Path.Combine(addon.ContentPath, "scripts/vscripts/alyxlib"))
            || Directory.Exists(Path.Combine(addon.ContentPath, "scripts/vscripts/alyxlib"))
            ;
    }

    /// <summary>
    /// Checks if the addon has an AlyxLib config file.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public static bool AddonHasConfig(LocalAddon addon)
    {
        return File.Exists(Path.Combine(addon.ContentPath, ".alyxlib/config.json"));
    }

    public static bool AddonHasConfig(LocalAddon addon, out AddonConfig config)
    {
        var configPath = Path.Combine(addon.ContentPath, ".alyxlib/config.json");
        if (File.Exists(configPath))
        {
            AddonConfig? _config = JsonSerializer.Deserialize<AddonConfig>(File.ReadAllText(configPath), AddonConfigJsonContext.Default.AddonConfig);
            if (_config == null)
            {
                config = new();
                return false;
            }

            config = _config;
            return true;
        }

        config = new();
        return false;
    }

    public static bool CheckPathIsAlyxLib(string path)
    {
        List<string> missingPaths = new();

        void checkList(SymlinkMap[] list)
        {
            foreach (SymlinkMap file in list)
                if (!Path.Exists(Path.Combine(path, file.From)))
                    missingPaths.Add(path);
        }

        if (!Directory.Exists(path)) return false;

        checkList(FileManager.vscriptFiles);
        checkList(FileManager.panoramaFiles);

        if (missingPaths.Count > 0)
            return false;

        return true;
    }

    public static async Task DownloadRepository(string extractPath, EventHandler<float> progressUpdate)
    {
        App.DebugConsoleVerbose("Downloading AlyxLib repository from GitHub");
        var zipUrl = $"{SettingsManager.Settings.GitHubUrl}/archive/refs/heads/main.zip";
        var zipFilePath = Path.Combine(Path.GetTempPath(), "alyxlib_repo.zip");

        //using HttpClient client = new HttpClient();
        //try
        //{
        //    var zipFilePath = Path.Combine(Path.GetTempPath(), "repo.zip");

        //    byte[] repoData = await client.GetByteArrayAsync(zipUrl);
        //    await File.WriteAllBytesAsync(zipFilePath, repoData);

        //    ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        //}
        //catch (Exception ex)
        //{
        //    App.DebugConsoleError($"Error: {ex.Message}");
        //}

        // Seting up the http client used to download the data
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(5);

            var progress = new Progress<float>();
            progress.ProgressChanged += progressUpdate;

            // Create a file stream to store the downloaded data.
            // This really can be any type of writeable stream.

            using (var file = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))

                // Use the custom extension method below to download the data.
                // The passed progress-instance will receive the download status updates.
                await client.DownloadAsync(zipUrl, file, progress);

            App.DebugConsoleVerbose($"Downloaded to: {zipFilePath}");

            ExtractWithoutTopLevelFolder(zipFilePath, extractPath);

            App.DebugConsoleVerbose($"Extracted to: {extractPath}");
            //ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        }

        // Delete temp zip
        // Avoid possible bad directory error using this check
        if (File.Exists(zipFilePath))
            File.Delete(zipFilePath);
    }

    /// <summary>
    /// Gets the AlyxLib installer config for the addon. If the config file does not exist, a blank config is returned.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public static AddonConfig GetAddonConfig(LocalAddon addon)
    {
        var configPath = Path.Combine(addon.ContentPath, ".alyxlib/config.json");
        if (File.Exists(configPath))
        {
            AddonConfig? config = JsonSerializer.Deserialize<AddonConfig>(File.ReadAllText(configPath), AddonConfigJsonContext.Default.AddonConfig);
            return config ?? new AddonConfig();
        }

        return new AddonConfig();
    }

    /// <summary>
    /// Saves the addon config to the addon's .alyxlib folder.
    /// </summary>
    /// <param name="addon"></param>
    /// <param name="config"></param>
    public static void SaveAddonConfig(LocalAddon addon, AddonConfig config)
    {
        var configPath = Path.Combine(addon.ContentPath, ".alyxlib/config.json");
        var folderPath = Path.GetDirectoryName(configPath);

        if (folderPath == null)
        {
            App.DebugConsoleError($"Failed to get folder path for config file: {configPath}");
            return;
        }

        // Create a hidden folder only if it doesn't exist (allows user to unhide it)
        if (!Directory.Exists(folderPath))
        {
            DirectoryInfo di = Directory.CreateDirectory(folderPath);
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, AddonConfigJsonContext.Default.AddonConfig));
    }

    /// <summary>
    /// Extracts a zip file to a directory without the top-level zip folder.
    /// </summary>
    /// <param name="zipPath"></param>
    /// <param name="extractTo"></param>
    static void ExtractWithoutTopLevelFolder(string zipPath, string extractTo)
    {
        using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            foreach (var entry in archive.Entries)
            {
                // Skip directories
                if (entry.FullName.EndsWith('/'))
                    continue;

                // Remove the top-level folder from the path
                var entryPath = entry.FullName;
                var pathParts = entryPath.Split('/', '\\');
                var relativePath = Path.Combine(pathParts[1..]); // Skip the first part (top-level folder)

                // Ensure the destination directory exists
                var targetPath = Path.Combine(extractTo, relativePath);
                string? path = Path.GetDirectoryName(targetPath) ?? throw new Exception($"Directory name for path '{targetPath}' invalid.");
                Directory.CreateDirectory(path);

                // Extract the file
                entry.ExtractToFile(targetPath, overwrite: true);
            }
    }

    public static bool StringIsValidModName(string input)
    {
        return !input.Any(c => !char.IsLetterOrDigit(c) && c != '_');
    }
}