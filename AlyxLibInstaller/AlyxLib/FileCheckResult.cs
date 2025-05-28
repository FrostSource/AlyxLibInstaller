#nullable enable

namespace AlyxLibInstaller.AlyxLib;

public enum FileCheckResult
{
    AlyxLibPathNotFound,
    Error,
    Warning,
    NotInstalled,
    PartiallyInstalled,
    FullyInstalled
}
