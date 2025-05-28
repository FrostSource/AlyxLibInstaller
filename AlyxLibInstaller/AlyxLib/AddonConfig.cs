#nullable enable

namespace AlyxLibInstaller.AlyxLib;

public class AddonConfig
{
    public bool VScriptInstalled { get; set; }
    public bool SoundEventInstalled { get; set; }
    public bool PanoramaInstalled { get; set; }
    public bool GitInstalled { get; set; }
    public string ModFolderName { get; set; } = string.Empty;
    public string Version { get; set; } = "0.0.0";
    public ScriptEditor EditorType { get; set; } = ScriptEditor.None;
}