using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace AlyxLib.Services;

public enum FileRemovalAction
{
    Delete,
    TemporaryMove
}

public readonly record struct FileRemovalMatch(string RelativePath, FileRemovalAction Action);

internal readonly record struct FileGlobRule(string Pattern, bool IsExclude, FileRemovalAction Action);

public class FileGlobService
{
    public const string TemporaryPrefix = "TMP:";

    private readonly IReadOnlyList<FileGlobRule> _rules;

    public event Action<string, Exception>? OnFileDeleteException;
    public event Action<string>? OnFileDelete;
    public event Action<string, string>? OnFileTemporaryMove;
    public event Action<string, Exception>? OnFileTemporaryMoveException;
    public event Action<string>? OnFileRestore;
    public event Action<string, Exception>? OnFileRestoreException;

    public FileGlobService(IEnumerable<string> globs)
    {
        _rules = globs
            .Select(ParseRule)
            .Where(rule => rule.HasValue)
            .Select(rule => rule!.Value)
            .ToList();
    }

    private static FileGlobRule? ParseRule(string? rawPattern)
    {
        if (string.IsNullOrWhiteSpace(rawPattern))
            return null;

        var pattern = rawPattern.Trim();
        var action = FileRemovalAction.Delete;

        if (pattern.StartsWith(TemporaryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            action = FileRemovalAction.TemporaryMove;
            pattern = pattern[TemporaryPrefix.Length..];
        }

        var isExclude = pattern.StartsWith('!');
        if (isExclude)
        {
            pattern = pattern[1..];
            action = FileRemovalAction.Delete;
        }

        if (string.IsNullOrWhiteSpace(pattern))
            return null;

        return new FileGlobRule(pattern, isExclude, action);
    }

    private static FileRemovalAction MergeActions(FileRemovalAction existingAction, FileRemovalAction nextAction)
    {
        return existingAction == FileRemovalAction.TemporaryMove || nextAction == FileRemovalAction.TemporaryMove
            ? FileRemovalAction.TemporaryMove
            : FileRemovalAction.Delete;
    }

    public IReadOnlyList<FileRemovalMatch> GetMatchingFileActions(string dir)
    {
        var includeRules = _rules.Where(rule => !rule.IsExclude).ToList();
        var excludeRules = _rules.Where(rule => rule.IsExclude).ToList();
        var directoryInfoWrapper = new DirectoryInfoWrapper(new DirectoryInfo(dir));
        var matchedFiles = new Dictionary<string, FileRemovalAction>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in includeRules)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(rule.Pattern);

            foreach (var file in matcher.Execute(directoryInfoWrapper).Files)
            {
                if (matchedFiles.TryGetValue(file.Path, out var existingAction))
                    matchedFiles[file.Path] = MergeActions(existingAction, rule.Action);
                else
                    matchedFiles[file.Path] = rule.Action;
            }
        }

        foreach (var rule in excludeRules)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(rule.Pattern);

            foreach (var file in matcher.Execute(directoryInfoWrapper).Files)
                matchedFiles.Remove(file.Path);
        }

        return [.. matchedFiles
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new FileRemovalMatch(pair.Key, pair.Value))];
    }

    public IEnumerable<string> GetMatchingFiles(string dir) =>
        GetMatchingFileActions(dir).Select(match => Path.Combine(dir, match.RelativePath));

    public int GetMatchingFileCount(string dir) => GetMatchingFileActions(dir).Count;

    public void ApplyRemovals(string dir, string temporaryStorageDir)
    {
        foreach (var match in GetMatchingFileActions(dir))
        {
            var sourcePath = Path.Combine(dir, match.RelativePath);

            try
            {
                switch (match.Action)
                {
                    case FileRemovalAction.Delete:
                        File.Delete(sourcePath);
                        OnFileDelete?.Invoke(sourcePath);
                        CleanupEmptyDirectories(Path.GetDirectoryName(sourcePath), dir);
                        break;
                    case FileRemovalAction.TemporaryMove:
                        var temporaryPath = Path.Combine(temporaryStorageDir, match.RelativePath);
                        var temporaryDirectory = Path.GetDirectoryName(temporaryPath);
                        if (!string.IsNullOrEmpty(temporaryDirectory))
                            Directory.CreateDirectory(temporaryDirectory);

                        if (File.Exists(temporaryPath))
                        {
                            throw new IOException(
                                $"Temporary storage already contains '{match.RelativePath}'. Install the addon to restore files before removing again.");
                        }

                        File.Move(sourcePath, temporaryPath);
                        OnFileTemporaryMove?.Invoke(sourcePath, temporaryPath);
                        CleanupEmptyDirectories(Path.GetDirectoryName(sourcePath), dir);
                        break;
                }
            }
            catch (Exception ex)
            {
                switch (match.Action)
                {
                    case FileRemovalAction.Delete:
                        OnFileDeleteException?.Invoke(sourcePath, ex);
                        break;
                    case FileRemovalAction.TemporaryMove:
                        OnFileTemporaryMoveException?.Invoke(sourcePath, ex);
                        break;
                }
            }
        }
    }

    public void RestoreTemporarilyMovedFiles(string dir, string temporaryStorageDir)
    {
        if (!Directory.Exists(temporaryStorageDir))
            return;

        foreach (var temporaryFile in Directory.GetFiles(temporaryStorageDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(temporaryStorageDir, temporaryFile);
            var destinationPath = Path.Combine(dir, relativePath);

            try
            {
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                if (File.Exists(destinationPath))
                    throw new IOException($"Cannot restore '{relativePath}' because the destination file already exists.");

                File.Move(temporaryFile, destinationPath);
                OnFileRestore?.Invoke(destinationPath);
                CleanupEmptyDirectories(Path.GetDirectoryName(temporaryFile), temporaryStorageDir);
            }
            catch (Exception ex)
            {
                OnFileRestoreException?.Invoke(destinationPath, ex);
            }
        }

        CleanupEmptyDirectories(temporaryStorageDir, temporaryStorageDir, deleteRoot: true);
    }

    private static void CleanupEmptyDirectories(string? startPath, string rootPath, bool deleteRoot = false)
    {
        if (string.IsNullOrWhiteSpace(startPath) || string.IsNullOrWhiteSpace(rootPath))
            return;

        var currentPath = Path.GetFullPath(startPath);
        var rootFullPath = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        while (currentPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            var isRoot = string.Equals(
                currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                rootFullPath,
                StringComparison.OrdinalIgnoreCase);

            if (isRoot && !deleteRoot)
                break;

            if (!Directory.Exists(currentPath) || Directory.EnumerateFileSystemEntries(currentPath).Any())
                break;

            Directory.Delete(currentPath);

            if (isRoot)
                break;

            var parent = Directory.GetParent(currentPath);
            if (parent == null)
                break;

            currentPath = parent.FullName;
        }
    }
}
