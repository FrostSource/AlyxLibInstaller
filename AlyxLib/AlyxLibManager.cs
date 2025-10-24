#nullable enable

using System.Diagnostics.CodeAnalysis;
using AlyxLib.Logging;

namespace AlyxLib;

public partial class AlyxLibManager
{
    public readonly VersionManager VersionManager;
    public readonly FileManager FileManager;

    public ILogger? Logger { get; set; } = new ConsoleLogger();
    public bool VerboseLogging { get; set; } = false;

    public PathInfo? AlyxLibPath { get; private set; } = null;

    [MemberNotNullWhen(true, nameof(AlyxLibPath))]
    public bool AlyxLibExists
    {
        get => AlyxLibPath != null && AlyxLibPath.Exists;
    }

    public string AlyxLibVersion { get; private set; } = "0.0.0";

    public AlyxLibManager()
    {
        VersionManager = new(this);
        FileManager = new(this);
    }

    public void SetAlyxLibPath(string path)
    {
        AlyxLibPath = new PathInfo(path);
        AlyxLibVersion = VersionManager.TryGetLocalVersion(out var version) ? version : "0.0.0";
    }

    [MemberNotNullWhen(false, nameof(AlyxLibPath))]
    public bool IssueFound()
    {
        if (AlyxLibPath == null || !AlyxLibExists)
        {
            Logger?.Log("AlyxLib path not set", LogType.Error, LogSeverity.High);
            return true;
        }

        return false;
    }
}
