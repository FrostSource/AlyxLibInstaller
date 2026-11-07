using System.ComponentModel;
using System.Diagnostics;

namespace AlyxLib;

public class GitHelper
{
    public static bool IsGitInstalled() => RunGit("--version");

    public static bool InitRepository(string folderPath) => RunGit("init", folderPath);

    public static bool FolderHasGitRepository(string path) => RunGit("rev-parse --is-inside-work-tree", path);

    /// <summary>
    /// Checks if the folder itself contains a .git folder, as opposed to being inside a parent repository.
    /// </summary>
    public static bool FolderIsRepositoryRoot(string path) => Directory.Exists(Path.Combine(path, ".git"));

    /// <summary>
    /// Deletes a .git folder. Git marks object and pack files as read-only, which makes a plain recursive delete fail part way.
    /// </summary>
    public static void DeleteGitFolder(string gitFolder)
    {
        foreach (var file in Directory.EnumerateFiles(gitFolder, "*", SearchOption.AllDirectories))
        {
            var attributes = File.GetAttributes(file);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
        }

        Directory.Delete(gitFolder, true);
    }

    public static bool CreateGitIgnore(string filePath, string content)
    {
        if (File.Exists(filePath)) return false;
        File.WriteAllText(filePath, content);
        return true;
    }

    /// <summary>
    /// Runs a git command and returns whether it succeeded.
    /// Returns false instead of throwing if git is not installed or the working directory doesn't exist.
    /// </summary>
    private static bool RunGit(string arguments, string? workingDirectory = null)
    {
        if (workingDirectory != null && !Directory.Exists(workingDirectory))
            return false;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            // git executable not found
            return false;
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
