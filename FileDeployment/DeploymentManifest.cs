using System.Text.Json;
using System.Text.Json.Serialization;

using FileDeployment.Converters;
using FileDeployment.Logging;

namespace FileDeployment
{
    public class DeploymentManifest
    {
        public IFileDeploymentLogger? Logger { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(ValidationRuleListConverter))]
        public List<ValidationRule>? DefaultRules { get; set; }
        public Dictionary<string, List<FileOperation>> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// A list of variable names that point to a Func that returns a string for that variable
        /// </summary>
        public Dictionary<string, Func<string>> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddVariable(string name, Func<string> value)
        {
            Variables[name] = value;
        }

        public void Log(LogEntry log)
        {
            Logger?.Log(log);
        }

        public void DeployCategory(string category)
        {
            if (!Categories.TryGetValue(category, out List<FileOperation>? operations))
                throw new KeyNotFoundException($"Category '{category}' not found in manifest");

            foreach (FileOperation operation in operations)
            {
                operation.ExecuteWithoutRules();
            }
        }

        public void DeployAllCategories()
        {
            foreach (List<FileOperation> category in Categories.Values)
            {
                foreach (FileOperation operation in category)
                {
                    operation.Execute();
                }
            }
        }

        public void ApplyDefaultChecks()
        {
            if (DefaultRules != null)
            {
                foreach (List<FileOperation> category in Categories.Values)
                {
                    foreach (FileOperation operation in category)
                    {
                        operation.Rules ??= [.. DefaultRules];
                    }
                }
            }
        }

        public void ApplyManifestContext()
        {
            foreach (List<FileOperation> category in Categories.Values)
            {
                foreach (FileOperation operation in category)
                {
                    operation.SetManifestContext(this);
                }
            }
        }

        public static DeploymentManifest LoadFromString(string content)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new VariableStringConverter() },
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var manifest = JsonSerializer.Deserialize<DeploymentManifest>(content, options)
                ?? throw new JsonException("Failed to deserialize manifest");

            manifest.ApplyDefaultChecks();
            manifest.ApplyManifestContext();

            return manifest;
        }

        public static DeploymentManifest LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Manifest file not found", path);
            
            return LoadFromString(File.ReadAllText(path));
        }
    }
}
