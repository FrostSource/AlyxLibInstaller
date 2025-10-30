using Semver;

namespace AlyxLib;
public sealed class VersionComparisonResult(int comparison, SemVersion localVersion, SemVersion remoteVersion)
{
    public int Comparison { get; init; } = comparison;
    public bool RemoteIsNewer => Comparison > 0;
    public bool LocalIsNewer => Comparison < 0;
    public bool SameVersion => Comparison == 0;
    public SemVersion LocalVersion { get; init; } = localVersion;
    public SemVersion RemoteVersion { get; init; } = remoteVersion;
    public bool RemoteConnectionFailed { get; init; } = false;

    public static VersionComparisonResult Empty => new(0, new SemVersion(0, 0, 0), new SemVersion(0, 0, 0));
    public static VersionComparisonResult Failed => new(0, new SemVersion(0, 0, 0), new SemVersion(0, 0, 0)) { RemoteConnectionFailed = true };
}
