using System.IO;
using System.Text.Json;

namespace AlyxLibInstaller;

public sealed class Settings
{
    private bool rememberLastAddon = false;
    public bool RememberLastAddon { get => rememberLastAddon; set { rememberLastAddon = value; SettingsManager.Save(); } }

    private string lastAddon = "";
    public string LastAddon { get => lastAddon; set { lastAddon = value; SettingsManager.Save(); } }

    private string theme = "";
    public string Theme { get => theme; set { theme = value; SettingsManager.Save(); } }

    private string alyxLibDirectory = "";
    public string AlyxLibDirectory { get => alyxLibDirectory; set { alyxLibDirectory = value; SettingsManager.Save(); } }

    private bool firstTimeRunning = true;
    public bool FirstTimeRunning { get => firstTimeRunning; set { firstTimeRunning = value; SettingsManager.Save(); } }

    private bool verboseConsole = false;
    public bool VerboseConsole { get => verboseConsole; set { verboseConsole = value; SettingsManager.Save(); } }

    private string gitHubVersionFileUrl = @"https://raw.githubusercontent.com/FrostSource/alyxlib/refs/heads/main/version.json";
    public string GitHubVersionFileUrl { get => gitHubVersionFileUrl; set { gitHubVersionFileUrl = value; SettingsManager.Save(); } }

    private string gitHubUrl = @"https://github.com/FrostSource/alyxlib";
    public string GitHubUrl { get => gitHubUrl; set { gitHubUrl = value; SettingsManager.Save(); } }
}

internal sealed class SettingsManager
{
    /// <summary>
    /// The global settings singleton.
    /// </summary>
    public static Settings Settings { get; private set; }
    private static bool ReadyToSave { get; set; } = false;

    static SettingsManager()
    {
        Settings = new();
        Load();
        ReadyToSave = true;
    }

    public static string Path => PathHelper.SettingsFilePath;

    public static void Save()
    {
        if (ReadyToSave)
        {
            string jsonString = JsonSerializer.Serialize(Settings, SettingsJsonContext.Default.Settings);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllText(Path, jsonString);
        }
    }

    public static void Load()
    {
        if (!File.Exists(Path))
        {
            return;
        }

        string jsonString = File.ReadAllText(Path);
        Settings = JsonSerializer.Deserialize(jsonString, SettingsJsonContext.Default.Settings);
    }
}