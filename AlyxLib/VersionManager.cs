using AlyxLib.Logging;
using Semver;
using System.Text.Json;

namespace AlyxLib
{
    public class VersionManager
    {
        private readonly AlyxLibManager manager;

        public ILogger? Logger => manager.Logger;

        //private static JsonSerializerOptions JsonSerializerOptions => new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public VersionManager(AlyxLibManager manager) => this.manager = manager;

        /// <summary>
        /// Compares the local version of AlyxLib with the latest version on GitHub.
        /// </summary>
        /// <returns>-1 if local version is newer, 0 if they're the same, 1 if GitHub version is new</returns>
        /// <exception cref="Exception"></exception>
        public async Task<VersionComparisonResult> CompareVersions(string githubVersionUrl)
        {
            //if (manager.IssueFound()) throw new Exception("AlyxLib path not found!");

            // If there is no version file, it's probably a pre-installer version
            if (!TryGetLocalVersion(out var localVersionString))
                Logger?.LogWarning("Local Version file not found, assuming pre-installer version", LogSeverity.Low);

            var localVersion = SemVersion.Parse(localVersionString);

            var remoteVersion = SemVersion.Parse(await GetRemoteVersion(githubVersionUrl));

            var comparison = SemVersion.ComparePrecedence(remoteVersion, localVersion);

            return new VersionComparisonResult(comparison, localVersion, remoteVersion);
        }

        /// <summary>
        /// Attempts to get the version of AlyxLib from the GitHub repository.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> GetRemoteVersion(string githubVersionUrl)
        {
            using var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(githubVersionUrl);
            //App.DebugConsoleMessage(GitHubVersionFileUrl);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get version file from GitHub: {response.StatusCode}");

            var remoteVersionContent = await response.Content.ReadAsStringAsync();
            VersionInfo? remoteJson = JsonSerializer.Deserialize(remoteVersionContent, VersionInfoJsonContext.Default.VersionInfo);
            return remoteJson == null
                ? throw new Exception($"Failed to parse version file from GitHub:\n{remoteVersionContent}")
                : remoteJson.Version;
        }

        /// <summary>
        /// Attempts to get the local version of AlyxLib from the version.json file.
        /// </summary>
        /// <param name="version"></param>
        /// <returns>The version string if successful, 0.0.0 otherwise</returns>
        /// <exception cref="Exception">AlyxLib was not found</exception>
        public bool TryGetLocalVersion(out string version)
        {
            //if (manager.IssueFound()) throw new Exception("AlyxLib path not found!");
            if (!manager.AlyxLibExists)
            {
                version = "0.0.0";
                return false;
            }

            var localVersionFile = Path.Combine(manager.AlyxLibPath.FullName, "version.json");

            if (!File.Exists(localVersionFile))
            {
                // If there is no version file, it's probably a pre-installer version
                version = "0.0.0";
                return false;
            }
            else
            {
                var localVersionContent = File.ReadAllText(localVersionFile);
                VersionInfo? localJson = JsonSerializer.Deserialize(localVersionContent, VersionInfoJsonContext.Default.VersionInfo);
                if (localJson == null)
                {
                    version = "0.0.0";
                    return false;
                }
                version = localJson.Version;
                return true;
            }
        }
    }
}
