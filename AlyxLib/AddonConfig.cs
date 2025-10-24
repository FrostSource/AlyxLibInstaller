namespace AlyxLib;

public class AddonConfig
{
    public bool VScriptInstalled { get; set; }
    public bool SoundEventInstalled { get; set; }
    public bool PanoramaInstalled { get; set; }
    public bool GitInstalled { get; set; }
    public string ModFolderName { get; set; } = string.Empty;
    public string Version { get; set; } = "0.0.0";
    public ScriptEditor EditorType { get; set; } = ScriptEditor.None;
    public List<string> FileRemovalGlobs { get; set; } = [];

    /// <summary>
    /// Returns true if the supplied boolean option is the only one true.
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public bool OnlyOption(bool option)
    {
        if (!option) return false;

        bool[] options = { VScriptInstalled, SoundEventInstalled, PanoramaInstalled, GitInstalled };

        int trueCount = 0;
        foreach (var o in options)
        {
            if (o) trueCount++;
            if (trueCount > 1) return false;
        }

        return true;
    }

    public bool IsFolderNameValid => AlyxLibHelpers.StringIsValidModName(ModFolderName);
}