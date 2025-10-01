using System.Text.Json.Serialization;

namespace AlyxLibInstaller.AlyxLib;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AddonConfig))]
public partial class AddonConfigJsonContext : JsonSerializerContext
{
}