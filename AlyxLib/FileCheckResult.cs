#nullable enable

namespace AlyxLib;

public enum FileCheckResult
{
    AlyxLibPathNotFound,
    Error,
    Warning,
    NotInstalled,
    PartiallyInstalled,
    FullyInstalled
}
