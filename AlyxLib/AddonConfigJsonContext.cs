using System.Text.Json.Serialization;

namespace AlyxLib;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AddonConfig))]
public partial class AddonConfigJsonContext : JsonSerializerContext
{
}