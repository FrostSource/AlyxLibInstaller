using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FileDeployment.Converters;

namespace FileDeployment;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowTrailingCommas = true, Converters =[ typeof(VariableStringConverter) ])]
[JsonSerializable(typeof(DeploymentManifest))]
[JsonSerializable(typeof(FileOperation))]
[JsonSerializable(typeof(ValidationRule))]
[JsonSerializable(typeof(VariableString))]
public partial class DeploymentManifestJsonContext : JsonSerializerContext
{
}