using System;
using System.Text.Json;
using System.IO;
using AlyxLibInstallerShared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AlyxLibInstallerShared;

public partial class Settings : ObservableObject
{
    //private bool rememberLastAddon = false;
    //public bool RememberLastAddon { get => rememberLastAddon; set { rememberLastAddon = value; SettingsManager.Save(); } }

    //private string lastAddon = "";
    //public string LastAddon { get => lastAddon; set { lastAddon = value; SettingsManager.Save(); } }

    //private AppTheme theme = AppTheme.System;
    //public AppTheme Theme { get => theme; set { theme = value; SettingsManager.Save(); } }

    //private string alyxLibDirectory = "";
    //public string AlyxLibDirectory { get => alyxLibDirectory; set { alyxLibDirectory = value; SettingsManager.Save(); } }

    //private bool firstTimeRunning = true;
    //public bool FirstTimeRunning { get => firstTimeRunning; set { firstTimeRunning = value; SettingsManager.Save(); } }

    //private bool verboseConsole = false;
    //public bool VerboseConsole { get => verboseConsole; set { verboseConsole = value; SettingsManager.Save(); } }

    //private string gitHubVersionFileUrl = @"https://raw.githubusercontent.com/FrostSource/alyxlib/refs/heads/main/version.json";
    //public string GitHubVersionFileUrl { get => gitHubVersionFileUrl; set { gitHubVersionFileUrl = value; SettingsManager.Save(); } }

    //private string gitHubUrl = @"https://github.com/FrostSource/alyxlib";
    //public string GitHubUrl { get => gitHubUrl; set { gitHubUrl = value; SettingsManager.Save(); } }

    [ObservableProperty]
    public partial bool RememberLastAddon { get; set; } = false;

    [ObservableProperty]
    public partial string LastAddon { get; set; } = "";

    [ObservableProperty]
    public partial AppTheme Theme { get; set; } = AppTheme.System;

    [ObservableProperty]
    public partial string AlyxLibDirectory { get; set; } = "";

    [ObservableProperty]
    public partial bool FirstTimeRunning { get; set; } = true;

    [ObservableProperty]
    public partial bool VerboseConsole { get; set; } = false;

    [ObservableProperty]
    public partial string GitHubVersionFileUrl { get; set; } = @"https://raw.githubusercontent.com/FrostSource/alyxlib/refs/heads/main/version.json";

    [ObservableProperty]
    public partial string GitHubUrl { get; set; } = @"https://github.com/FrostSource/alyxlib";

    [ObservableProperty]
    public partial ushort MaxLogFiles { get; set; } = 10;

    [ObservableProperty]
    public partial bool AutoDeleteLogFiles { get; set; } = true;

    [ObservableProperty]
    public partial Dictionary<string, bool> DontShowAgain { get; set; } = new();

    /// <summary>
    /// Sets a value in the "DontShowAgain" dictionary and raises the "DontShowAgain" property changed event.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetDontShowAgain(string key, bool value)
    {
        DontShowAgain[key] = value;
        OnPropertyChanged(nameof(DontShowAgain));
    }

    /// <summary>
    /// Gets if a value is set in the "DontShowAgain" dictionary.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool GetDontShowAgain(string key) => DontShowAgain.TryGetValue(key, out bool value) && value;
}

public sealed class SettingsManager
{
    /// <summary>
    /// The global settings singleton.
    /// </summary>
    public static Settings Settings { get; private set; }
    private static bool ReadyToSave { get; set; } = false;

    static SettingsManager()
    {
        Settings = Load();
        //ReadyToSave = true;

        Settings.PropertyChanged += (_, _) => Save();
    }

    public static string Path => PathHelper.SettingsFilePath;

    public static event EventHandler<Exception>? ErrorOccurred;

    private static void OnError(Exception ex) => ErrorOccurred?.Invoke(null, ex);

    public static void Save()
    {
        //if (ReadyToSave)
        try
        {
            string jsonString = JsonSerializer.Serialize(Settings, SettingsJsonContext.Default.Settings);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, jsonString);
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
    }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                var jsonString = File.ReadAllText(Path);
                var settings = JsonSerializer.Deserialize(jsonString, SettingsJsonContext.Default.Settings);
            
                if (settings != null) return settings;
            }
        }
        catch (Exception ex)
        {
            OnError(ex);
        }

        return new Settings();
    }
}