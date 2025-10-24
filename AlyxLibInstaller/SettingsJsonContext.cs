using System.Text.Json.Serialization;

namespace AlyxLibInstaller;

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true)]
// This attribute tells the source generator to create serialization metadata for Settings
[JsonSerializable(typeof(Settings))]
public partial class SettingsJsonContext : JsonSerializerContext
{
}