using AlyxLib.Extensions;
using AlyxLib.Logging;
using Source2HelperLibrary;
using System.IO.Compression;
using System.Text.Json;

namespace AlyxLib;

public static class AlyxLibHelpers
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
                    missingPaths.Add(file.From);
        }

        if (!Directory.Exists(path)) return false;

        checkList(FileManager.vscriptFiles);
        checkList(FileManager.panoramaFiles);

        if (missingPaths.Count > 0)
            return false;

        return true;
    }

    public static async Task DownloadRepository(string githubUrl, string extractPath, EventHandler<float>? progressUpdate = null, ILogger? logger = null)
    {
        logger?.Log("Downloading AlyxLib repository from GitHub", LogType.Basic, LogSeverity.Low);
        var zipUrl = $"{githubUrl}/archive/refs/heads/main.zip";
        var zipFilePath = Path.Combine(Path.GetTempPath(), $"alyxlib_repo_{Guid.NewGuid():N}.zip");

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

            logger?.Log($"Downloaded to: {zipFilePath}", LogType.Basic, LogSeverity.Low);

            try
            {
                var extractedFiles = ExtractWithoutTopLevelFolder(zipFilePath, extractPath);
                RemoveFilesNoLongerInRepository(extractPath, extractedFiles, logger);

                logger?.Log($"Extracted to: {extractPath}", LogType.Basic, LogSeverity.Low);
            }
            finally
            {
                // Delete temp zip
                // Avoid possible bad directory error using this check
                if (File.Exists(zipFilePath))
                    File.Delete(zipFilePath);
            }
        }
    }

    /// <summary>
    /// File in the AlyxLib folder listing the files extracted by the last download.
    /// </summary>
    public const string ExtractedFilesListName = ".installer_files.txt";

    /// <summary>
    /// Deletes files from the previous download that are no longer in the repository, so removed
    /// scripts don't stay linked into addons. Only files listed by the previous download are touched,
    /// so anything the user added to the folder is kept.
    /// </summary>
    internal static void RemoveFilesNoLongerInRepository(string extractPath, IReadOnlyCollection<string> extractedFiles, ILogger? logger)
    {
        var listPath = Path.Combine(extractPath, ExtractedFilesListName);

        if (File.Exists(listPath))
        {
            var currentFiles = new HashSet<string>(extractedFiles, StringComparer.OrdinalIgnoreCase);
            var extractRoot = Path.GetFullPath(extractPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            foreach (var oldFile in File.ReadAllLines(listPath))
            {
                if (string.IsNullOrWhiteSpace(oldFile) || currentFiles.Contains(oldFile))
                    continue;

                var oldFilePath = Path.GetFullPath(Path.Combine(extractPath, oldFile));
                if (!oldFilePath.StartsWith(extractRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(oldFilePath))
                    continue;

                try
                {
                    File.Delete(oldFilePath);
                    logger?.Log($"Removed file no longer in AlyxLib: {oldFile}", LogType.Basic, LogSeverity.Low);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning($"Could not remove old AlyxLib file {oldFile}: {ex.Message}");
                }
            }
        }

        File.WriteAllLines(listPath, extractedFiles);
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
    /// Extracts a zip file to a directory without the top-level zip folder.
    /// </summary>
    /// <param name="zipPath"></param>
    /// <param name="extractTo"></param>
    /// <returns>Relative paths of the extracted files.</returns>
    static List<string> ExtractWithoutTopLevelFolder(string zipPath, string extractTo)
    {
        var extractedFiles = new List<string>();

        using ZipArchive archive = ZipFile.OpenRead(zipPath);
        foreach (ZipArchiveEntry entry in archive.Entries)
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
            extractedFiles.Add(relativePath);
        }

        return extractedFiles;
    }

    public static bool StringIsValidModName(string? input)
    {
        return !string.IsNullOrWhiteSpace(input)
            && input.All(c => (char.IsLower(c) || char.IsDigit(c) || c == '_'));
    }
}