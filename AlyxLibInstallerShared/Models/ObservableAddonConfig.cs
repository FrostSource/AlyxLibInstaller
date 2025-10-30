using AlyxLib;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlyxLibInstallerShared.Models;
public class ObservableAddonConfig : ObservableObject
{
    private readonly AddonConfig config;

    public ObservableAddonConfig(AddonConfig config) => this.config = config;

    public string ModFolderName
    {
        get => config.ModFolderName;
        set
        {
            if (SetProperty(config.ModFolderName, value, config, (u, n) => u.ModFolderName = n))
            {
                OnPropertyChanged(nameof(IsFolderNameValid));
            }
        }
    }

    public bool VScriptInstalled
    {
        get => config.VScriptInstalled;
        set => SetProperty(config.VScriptInstalled, value, config, (u, n) => u.VScriptInstalled = n);
    }
    public bool SoundEventInstalled
    {
        get => config.SoundEventInstalled;
        set => SetProperty(config.SoundEventInstalled, value, config, (u, n) => u.SoundEventInstalled = n);
    }
    public bool PanoramaInstalled
    {
        get => config.PanoramaInstalled;
        set => SetProperty(config.PanoramaInstalled, value, config, (u, n) => u.PanoramaInstalled = n);
    }
    public bool GitInstalled
    {
        get => config.GitInstalled;
        set => SetProperty(config.GitInstalled, value, config, (u, n) => u.GitInstalled = n);
    }
    public string Version
    {
        get => config.Version;
        set => SetProperty(config.Version, value, config, (u, n) => u.Version = n);
    }
    public ScriptEditor EditorType
    {
        get => config.EditorType;
        set => SetProperty(config.EditorType, value, config, (u, n) => u.EditorType = n);
    }
    public List<string> FileRemovalGlobs
    {
        get => config.FileRemovalGlobs;
        set => SetProperty(config.FileRemovalGlobs, value, config, (u, n) => u.FileRemovalGlobs = n);
    }

    public bool OnlyOption(bool option) => config.OnlyOption(option);

    public bool IsFolderNameValid => config.IsFolderNameValid;

    public AddonConfig GetConfig() => config;
}
