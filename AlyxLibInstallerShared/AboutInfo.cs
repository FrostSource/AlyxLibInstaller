using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared;
public record AboutInfo
{
    public string AppName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Copyright { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public string Website { get; init; } = string.Empty;
    public string[] BuiltWith { get; init; } = [];
}
