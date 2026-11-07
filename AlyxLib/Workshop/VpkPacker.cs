using AlyxLib.Services;
using FileDeployment;
using Source2HelperLibrary;
using System.Diagnostics;
using System.Text;

namespace AlyxLib.Workshop;

/// <summary>
/// A file to pack, from its path on disk to its path inside the VPK.
/// </summary>
/// <param name="PackPath">The path inside the VPK, using forward slashes.</param>
public record PackFile(string SourcePath, string PackPath);

public class VpkPackException(string message, string output) : Exception(message)
{
    /// <summary>
    /// What vpk.exe printed, to help work out why packing failed.
    /// </summary>
    public string Output { get; } = output;
}

/// <summary>
/// Packs an addon's game folder into a VPK with vpk.exe, from a list of files so nothing on disk has to be moved first.
/// </summary>
/// <param name="runVpk">Runs vpk.exe with the given arguments and returns its exit code and output. Replaceable for testing.</param>
public class VpkPacker(string vpkExePath, Func<string, (int ExitCode, string Output)>? runVpk = null)
{
    /// <summary>
    /// Files the Workshop tools leave out of a pack.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultExcludes =
    [
        "tools_asset_info.bin",
        "tools_thumbnail_cache.bin",
        "_bakeresourcecache/**"
    ];

    public string VpkExePath { get; } = vpkExePath;

    public static VpkPacker ForInstalledGame() => new(Path.Combine(HLA.GetInstallFolder(), "game", "bin", "win64", "vpk.exe"));

    /// <summary>
    /// Gets the files that would be packed from an addon's game folder.
    /// Links are followed like the Workshop tools do.
    /// </summary>
    /// <param name="removalGlobs">The addon's upload removal rules. Matching files are left out.</param>
    /// <param name="excludedPaths">
    /// Files and folders to leave out, such as AlyxLib, which isn't shipped with the addon.
    /// Matched against both the path in the addon and the real path a link points to.
    /// </param>
    public static IReadOnlyList<PackFile> GetFiles(string gamePath, IEnumerable<string> removalGlobs, IEnumerable<string> excludedPaths)
    {
        var files = new List<PackFile>();
        var fullGamePath = Path.GetFullPath(gamePath);
        var visitedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { fullGamePath };
        var fullExcludedPaths = excludedPaths.Where(path => !string.IsNullOrEmpty(path)).Select(Path.GetFullPath).ToList();
        AddFiles(new DirectoryInfo(fullGamePath), fullGamePath, "", fullExcludedPaths, files, visitedFolders);

        var packPaths = files.Select(file => file.PackPath).ToList();
        var excluded = new FileGlobService(DefaultExcludes).MatchRelativePaths(packPaths)
            .Union(new FileGlobService(removalGlobs).MatchRelativePaths(packPaths), StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. files
            .Where(file => !excluded.Contains(file.PackPath))
            .OrderBy(file => file.PackPath, StringComparer.OrdinalIgnoreCase)];
    }

    /// <param name="realFolder">Where <paramref name="folder"/> really is, after following any links to it.</param>
    private static void AddFiles(DirectoryInfo folder, string realFolder, string packFolder, List<string> excludedPaths, List<PackFile> files, HashSet<string> visitedFolders)
    {
        foreach (var entry in folder.EnumerateFileSystemInfos())
        {
            var packPath = packFolder.Length == 0 ? entry.Name : $"{packFolder}/{entry.Name}";
            var realPath = Path.Combine(realFolder, entry.Name);

            if (IsExcluded(entry.FullName, excludedPaths) || IsExcluded(realPath, excludedPaths))
                continue;

            if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                var target = entry.ResolveLinkTarget(returnFinalTarget: true);
                // Broken links can't be packed
                if (target == null || !target.Exists)
                    continue;

                realPath = Path.GetFullPath(target.FullName);
                if (IsExcluded(realPath, excludedPaths))
                    continue;
            }

            if (entry is DirectoryInfo directory)
            {
                // Guards against links that loop back on themselves
                if (visitedFolders.Add(realPath))
                    AddFiles(directory, realPath, packPath, excludedPaths, files, visitedFolders);
            }
            else
            {
                files.Add(new PackFile(entry.FullName, packPath));
            }
        }
    }

    private static bool IsExcluded(string path, List<string> excludedPaths) =>
        excludedPaths.Any(excluded => FileUtils.PathsAreEqual(path, excluded) || FileUtils.IsInsideFolder(path, excluded));

    /// <summary>
    /// Checks vpk.exe can be run, e.g. before creating a Workshop item that the pack is for.
    /// </summary>
    /// <exception cref="VpkPackException">vpk.exe isn't installed.</exception>
    public void EnsureAvailable()
    {
        if (runVpk == null && !File.Exists(VpkExePath))
            throw new VpkPackException($"vpk.exe wasn't found at {VpkExePath}. Make sure the Half-Life: Alyx Workshop Tools are installed.", "");
    }

    /// <summary>
    /// Packs the files into a new VPK, replacing any existing one.
    /// </summary>
    /// <exception cref="VpkPackException">vpk.exe is missing, failed or didn't create the VPK.</exception>
    public void Pack(IReadOnlyList<PackFile> files, string vpkPath)
    {
        if (files.Count == 0)
            throw new VpkPackException("There are no files to pack.", "");

        EnsureAvailable();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(vpkPath))!);

        // vpk.exe updates an existing pack in place, so it's removed to build a clean one
        if (File.Exists(vpkPath))
            File.Delete(vpkPath);

        var controlFile = Path.Combine(Path.GetTempPath(), $"alyxlib_vpk_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(controlFile, CreateControlFile(files));

            var (exitCode, output) = (runVpk ?? RunVpk)($"k \"{Path.GetFullPath(vpkPath)}\" \"{controlFile}\"");

            if (exitCode != 0 || !File.Exists(vpkPath))
                throw new VpkPackException($"vpk.exe failed to create '{vpkPath}' (exit code {exitCode}).", output);
        }
        finally
        {
            File.Delete(controlFile);
        }
    }

    /// <summary>
    /// Creates a vpk.exe KeyValues control file listing each file's source and pack path.
    /// </summary>
    public static string CreateControlFile(IEnumerable<PackFile> files)
    {
        var builder = new StringBuilder("\"files\"\n{\n");
        foreach (var file in files)
        {
            builder.Append($"\t\"{Escape(file.PackPath)}\"\n\t{{\n");
            builder.Append($"\t\t\"srcpath\"\t\"{Escape(file.SourcePath)}\"\n");
            builder.Append($"\t\t\"destpath\"\t\"{Escape(file.PackPath)}\"\n");
            builder.Append("\t}\n");
        }
        return builder.Append("}\n").ToString();
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private (int ExitCode, string Output) RunVpk(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(VpkExePath, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;

        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output + error.Result);
    }
}
