using Semver;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlyxLibInstaller.AlyxLib
{
    public class VersionManager
    {
        private readonly AlyxLibManager manager;

        private static JsonSerializerOptions JsonSerializerOptions => new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

        public VersionManager(AlyxLibManager manager) => this.manager = manager;

        /// <summary>
        /// Compares the local version of AlyxLib with the latest version on GitHub.
        /// </summary>
        /// <returns>-1 if local version is newer, 0 if they're the same, 1 if GitHub version is new</returns>
        /// <exception cref="Exception"></exception>
        public async Task<(int, SemVersion, SemVersion)> CompareVersions()
        {
            if (manager.IssueFound()) throw new Exception("AlyxLib path not found!");

            if (!TryGetLocalVersion(out var localVersionString))
                // If there is no version file, it's probably a pre-installer version
                App.DebugConsoleVerboseWarning("Local Version file not found, assuming pre-installer version");

            var localVersion = SemVersion.Parse(localVersionString);

            var remoteVersion = SemVersion.Parse(await GetRemoteVersion());

            return (SemVersion.ComparePrecedence(remoteVersion, localVersion), localVersion, remoteVersion);
        }

        /// <summary>
        /// Attempts to get the version of AlyxLib from the GitHub repository.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> GetRemoteVersion()
        {
            using var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(SettingsManager.Settings.GitHubVersionFileUrl);
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
            if (manager.IssueFound()) throw new Exception("AlyxLib path not found!");

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
