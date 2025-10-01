using System.Text.Json.Serialization;

namespace AlyxLibInstaller.AlyxLib;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
)]
[JsonSerializable(typeof(VersionInfo))]
public partial class VersionInfoJsonContext : JsonSerializerContext
{
}