using System.Security.Cryptography;
using System.Text;

namespace AlyxLibInstallerShared;

public static class Utils
{
    public static string GetFileHash(string filePath)
    {
        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(filePath))
        {
            var hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public static string GetStringHash(string content)
    {
        // Convert the input string to a byte array and compute the hash.
        using (var sha256Hash = SHA256.Create())
        {
            // Convert the input string to a byte array and compute the hash.
            var hashBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(content));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    //public static string FindLongestValidPath(string candidate)
    //{
    //    // Clean candidate (remove newlines and trim whitespace)
    //    candidate = candidate.Replace("\r\n", "").Replace("\n", "").Trim();

    //    // Check if the full string is a valid path first
    //    if (Directory.Exists(candidate) || File.Exists(candidate))
    //        return candidate;

    //    // If not, start from the full string and work backwards
    //    for (int endPos = candidate.Length; endPos > 2; endPos--)
    //    {
    //        string currentCandidate = candidate.Substring(0, endPos).Trim();

    //        if (Directory.Exists(currentCandidate) || File.Exists(currentCandidate))
    //            return currentCandidate;
    //    }

    //    return null; // no valid path found
    //}
    public static string FindLongestValidPath(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        // Check if the full string is a valid path first
        if (Directory.Exists(candidate) || File.Exists(candidate))
            return candidate;

        // Try removing trailing characters one by one
        for (int endPos = candidate.Length - 1; endPos > 2; endPos--)
        {
            string currentCandidate = candidate.Substring(0, endPos);

            if (Directory.Exists(currentCandidate) || File.Exists(currentCandidate))
                return currentCandidate;
        }

        // If nothing found, try to at least return the drive root if it exists
        if (candidate.Length >= 3 && candidate[1] == ':' && candidate[2] == '\\')
        {
            string driveRoot = candidate.Substring(0, 3);
            if (Directory.Exists(driveRoot))
                return driveRoot;
        }

        return null;
    }

    /// <summary>
    /// Checks if the OS is Windows 11 or greater.
    /// Not accurate but good enough for our purposes.
    /// </summary>
    /// <returns></returns>
    public static bool IsWindows11OrGreater() =>
        OperatingSystem.IsWindows() &&
        Environment.OSVersion.Version.Major >= 10 &&
        Environment.OSVersion.Version.Build >= 22000;
}