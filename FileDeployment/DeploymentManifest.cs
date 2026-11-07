using FileDeployment.Converters;
using FileDeployment.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        /// Folders that are never removed when cleaning up empty folders after undeploying.
        /// Empty parent folders are removed up to, but not including, these folders.
        /// </summary>
        [JsonIgnore]
        public List<string> ProtectedFolders { get; set; } = [];
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
                switch (operation.Execute())
                {
                    case FileOperationExecutionResult.Success: success++; break;
                    case FileOperationExecutionResult.Failed: fail++; break;
                    case FileOperationExecutionResult.Skipped: break;
                }
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

        public DeploymentResult UndeployCategory(string category, bool ignoreRemoveFlags = false)
        {
            if (!Categories.ContainsKey(category))
                throw new KeyNotFoundException($"Category '{category}' not found in manifest");

            int success = 0, fail = 0;
            List<FileOperation> operations = Categories[category];
            foreach (FileOperation operation in operations)
            {
                if (operation.Remove == true || ignoreRemoveFlags)
                {
                    if (!operation.DeployedFileExists())
                    {
                        Log(LogEntry.Info(operation, "File does not exist, skipping..."));
                        continue;
                    }

                    if (!RemoveChangedFiles && !operation.DeployedFileIsUnchanged())
                    {
                        Log(LogEntry.Info(operation, "File is modified, skipping..."));
                        fail++;
                        continue;
                    }

                    string deploymentPath = operation.GetDeploymentPath();
                    if (!string.IsNullOrEmpty(deploymentPath))
                    {
                        try
                        {
                            string? containingFolder = FileUtils.GetContainingFolder(deploymentPath);

                            if (File.Exists(deploymentPath)) { File.Delete(deploymentPath); }
                            else if (Directory.Exists(deploymentPath)) { Directory.Delete(deploymentPath); }
                            Log(new LogEntry(operation, $"Removed path '{deploymentPath}'") { Type = LogEntryType.Info });

                            if (!string.IsNullOrEmpty(containingFolder))
                                RemoveEmptyParentFolders(operation, containingFolder);

                            success++;
                        }
                        catch (Exception ex)
                        {
                            Log(LogEntry.Error(operation, $"Failed to remove path '{deploymentPath}'", ex));
                            fail++;
                        }
                    }
                }
                else
                {
                    Log(LogEntry.Info(operation, "Operation is not marked for removal in deployment manifest, skipping..."));
                }
            }

            return new DeploymentResult(success, fail);
        }

        /// <summary>
        /// Removes <paramref name="folder"/> if it is empty, then each parent that becomes empty, stopping at the
        /// first protected folder containing it. Without a protected folder only <paramref name="folder"/> is removed.
        /// Folders containing anything at all, including hidden files, and symlinked folders are never removed.
        /// </summary>
        /// <param name="onlyInsideProtectedFolder">If true, nothing is removed unless a protected folder contains <paramref name="folder"/>.</param>
        internal void RemoveEmptyParentFolders(FileOperation operation, string folder, bool onlyInsideProtectedFolder = false)
        {
            string current = Path.GetFullPath(folder);
            string? root = ProtectedFolders
                .Select(Path.GetFullPath)
                .FirstOrDefault(protectedFolder => FileUtils.IsInsideFolder(current, protectedFolder));

            if (root == null && onlyInsideProtectedFolder)
                return;

            while (Path.GetPathRoot(current) != current
                && Directory.Exists(current)
                && !ProtectedFolders.Any(protectedFolder => FileUtils.PathsAreEqual(protectedFolder, current))
                && !new DirectoryInfo(current).Attributes.HasFlag(FileAttributes.ReparsePoint)
                && !Directory.EnumerateFileSystemEntries(current).Any())
            {
                try
                {
                    // Not recursive, so this fails rather than deleting anything created since the check
                    Directory.Delete(current);
                    Log(LogEntry.Info(operation, $"Removed empty parent folder '{current}'"));
                }
                catch (Exception ex)
                {
                    Log(LogEntry.Error(operation, $"Failed to remove empty parent folder '{current}'", ex));
                    return;
                }

                string? parent = Path.GetDirectoryName(current);
                if (root == null || parent == null || !FileUtils.IsInsideFolder(parent, root))
                    return;

                current = parent;
            }
        }

        public bool TryUndeployCategory(string category, out DeploymentResult result, bool ignoreRemoveFlags = false)
        {
            try
            {
                result = UndeployCategory(category, ignoreRemoveFlags: ignoreRemoveFlags);
                return true;
            }
            catch
            {
                result = new DeploymentResult(0, 0);
                return false;
            }
        }

        public string[] GetCategoryDestinations(string category)
        {
            if (!Categories.ContainsKey(category))
                throw new KeyNotFoundException($"Category '{category}' not found in manifest");

            var destinations = new List<string>();

            foreach (FileOperation operation in Categories[category])
            {
                if (operation is IFileOperationWithDestination operationWithDestination)
                {
                    string? destination = operationWithDestination.Destination?.ToString();
                    if (!string.IsNullOrEmpty(destination))
                        destinations.Add(destination);
                }
            }

            return destinations.ToArray();
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

            // Deserializing replaces the dictionary, losing its case insensitive comparer
            manifest.Categories = new(manifest.Categories, StringComparer.OrdinalIgnoreCase);

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
