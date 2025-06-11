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

        /// <summary>
        /// Gets or sets a value indicating whether existing symbolic links should be replaced.
        /// If false, existing symbolic links will not be replaced, and an exception will be logged.
        /// </summary>
        public bool ReplaceExistingSymlinks { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether existing files should be replaced during the operation.
        /// If false, existing files will not be replaced, and an exception will be logged.
        /// </summary>
        public bool ReplaceExistingFiles { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether operations should ignore missing source files.
        /// If true, operations will not throw an exception if the source file is missing. The operation will be skipped.
        /// </summary>
        public bool IgnoreMissingSource { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether operations should ignore missing destination files.
        /// If true, operations will not throw an exception if the destination file is missing. The operation will be skipped.
        /// <para>This only applies to <see cref="FileOperation"/> interfacing <see cref="IFileOperationWithDestination"/>.</para>
        /// </summary>
        public bool IgnoreMissingDestination { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether changed files should be removed when undeploying.
        /// Changed files are those that do not match what the deployed file would look like.
        /// </summary>
        public bool RemoveChangedFiles { get; set; } = false;

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

        private DeploymentResult DeployCategoryInternal(string category)
        {
            List<FileOperation> operations = Categories[category];
            int success = 0, fail = 0;
            foreach (FileOperation operation in operations)
            {
                if (operation.Execute())
                    success++;
                else
                    fail++;
            }

            return new DeploymentResult(success, fail);
        }

        public DeploymentResult DeployCategory(string category)
        {
            if (!Categories.ContainsKey(category))
                throw new KeyNotFoundException($"Category '{category}' not found in manifest");

            return DeployCategoryInternal(category);
        }

        public bool TryDeployCategory(string category, out DeploymentResult result)
        {
            try
            {
                result = DeployCategoryInternal(category);
                return true;
            }
            catch
            {
                result = new DeploymentResult(0, 0);
                return false;
            }
        }

        public bool HasCategory(string category)
        {
            return Categories.ContainsKey(category);
        }

        public Dictionary<string, DeploymentResult> DeployAllCategories()
        {
            Dictionary<string, DeploymentResult> results = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string name, List<FileOperation> category) in Categories)
            {
                //foreach (FileOperation operation in category)
                //{
                //    operation.Execute();
                //}
                var result = DeployCategory(name);
                results[name] = result;
            }
            return results;
        }

        public DeploymentResult UndeployCategory(string category)
        {
            if (!Categories.ContainsKey(category))
                throw new KeyNotFoundException($"Category '{category}' not found in manifest");
            
            int success = 0, fail = 0;
            List<FileOperation> operations = Categories[category];
            foreach (FileOperation operation in operations)
            {
                if (operation.Remove == true)
                {
                    if (RemoveChangedFiles || operation.DeployedFileIsUnchanged())
                    {
                        string deploymentPath = operation.GetDeploymentPath();
                        if (!string.IsNullOrEmpty(deploymentPath))
                        {
                            try
                            {
                                if (File.Exists(deploymentPath)) { File.Delete(deploymentPath); }
                                else if (Directory.Exists(deploymentPath)) { Directory.Delete(deploymentPath); }
                                Log(new LogEntry(operation, $"Removed path '{deploymentPath}'") { Type = LogEntryType.Info });
                                success++;
                            }
                            catch (Exception ex)
                            {
                                Log(LogEntry.Error(operation, $"Failed to remove path '{deploymentPath}'", ex));
                                fail++;
                            }
                        }
                    }
                }
            }
            
            return new DeploymentResult(success, fail);
        }

        public bool TryUndeployCategory(string category, out DeploymentResult result)
        {
            try
            {
                result = UndeployCategory(category);
                return true;
            }
            catch
            {
                result = new DeploymentResult(0, 0);
                return false;
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
                WriteIndented = true,
                AllowTrailingCommas = true,
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
