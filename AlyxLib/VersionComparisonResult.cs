using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLib;
public sealed class VersionComparisonResult(int comparison, SemVersion localVersion, SemVersion remoteVersion)
{
    public int Comparison { get; init; } = comparison;
    public bool RemoteIsNewer => Comparison > 0;
    public bool LocalIsNewer => Comparison < 0;
    public bool SameVersion => Comparison == 0;
    public SemVersion LocalVersion { get; init; } = localVersion;
    public SemVersion RemoteVersion { get; init; } = remoteVersion;
}
