using FileDeployment.Converters;
using System.Text.Json.Serialization;

namespace FileDeployment;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, Converters = [typeof(VariableStringConverter)])]
[JsonSerializable(typeof(DeploymentManifest))]
[JsonSerializable(typeof(FileOperation))]
[JsonSerializable(typeof(ValidationRule))]
[JsonSerializable(typeof(VariableString))]
public partial class DeploymentManifestJsonContext : JsonSerializerContext
{
}