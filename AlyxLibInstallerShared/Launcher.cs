using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared;
public static class Launcher
{
    public static void Launch(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Target cannot be null or empty.", nameof(target));

        // Try URL first
        if (Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto))
        {
            LaunchInternal(uri.AbsoluteUri);
            return;
        }

        // Folder
        if (Directory.Exists(target))
        {
            LaunchInternal(target);
            return;
        }

        // File
        if (File.Exists(target))
        {
            Process.Start("explorer.exe", $"/select,\"{target}\"");
            return;
        }

        // Fallback: parent directory
        string? parentDir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
        {
            LaunchInternal(parentDir);
            return;
        }

        throw new FileNotFoundException($"Target not found: {target}");
    }

    public static bool TryLaunch(string target)
    {
        try
        {
            Launch(target);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void LaunchInternal(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true // important for default app / explorer
        };
        Process.Start(psi);
    }
}
