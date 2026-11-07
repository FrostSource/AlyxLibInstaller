using AlyxLibInstallerShared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace AlyxLibInstallerShared;

public partial class Settings : ObservableObject
{
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
    static SettingsManager()
    {
        Settings = Load();

        Settings.PropertyChanged += (_, _) => Save();
    }

    public static string Path => PathHelper.SettingsFilePath;

    public static event EventHandler<Exception>? ErrorOccurred;

    private static void OnError(Exception ex) => ErrorOccurred?.Invoke(null, ex);

    public static void Save()
    {
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