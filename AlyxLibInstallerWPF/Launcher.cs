using System.Diagnostics;

namespace AlyxLibInstallerWPF;

public static class Launcher
{
    public static Task<bool> LaunchUriAsync(string uri)
    {
        return Task.FromResult(Launch(uri));
    }

    public static Task<bool> LaunchUriAsync(Uri uri)
    {
        return Task.FromResult(Launch(uri.ToString()));
    }

    public static Task<bool> LaunchFolderPathAsync(string folderPath)
    {
        return Task.FromResult(Launch(folderPath));
    }

    private static bool Launch(string pathOrUri)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pathOrUri,
                UseShellExecute = true // important for default app / explorer
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

