namespace AlyxLibInstallerShared
{
    /// <summary>
    /// Centralized helper for application data and log paths.
    /// </summary>
    public static class PathHelper
    {
        public const string AppName = "AlyxLibInstaller";

        /// <summary>
        /// Gets the root program data directory for machine-wide data like logs.
        /// </summary>
        public static string ProgramDataPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName);

        /// <summary>
        /// Gets the root application data directory for user-specific settings.
        /// </summary>
        public static string UserDataPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

        /// <summary>
        /// Gets the logs directory path.
        /// </summary>
        public static string LogsPath => Path.Combine(ProgramDataPath, "logs");

        /// <summary>
        /// Gets the settings file path.
        /// </summary>
        public static string SettingsFilePath => Path.Combine(UserDataPath, "settings.json");
    }
}