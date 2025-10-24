using FileDeployment.Converters;
using FileDeployment.Exceptions;
using FileDeployment.Logging;
using System.Text.Json.Serialization;

namespace FileDeployment
{
    [JsonConverter(typeof(FileOperationConverter))]
    public abstract class FileOperation : IManifestContext
    {
        //public abstract OperationType Type { get; }

        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VariableString Source { get; set; }

        public virtual bool SkipIfSourceDoesNotExist => false;

        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public VariableString? Destination { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(ValidationRuleListConverter))]
        public List<ValidationRule>? Rules { get; set; }

        public DeploymentManifest? Manifest { get; private set; }
        public virtual void SetManifestContext(DeploymentManifest manifest)
        {
            Manifest = manifest;

            Source?.SetManifestContext(manifest);

            if (this is IFileOperationWithDestination operationWithDestination)
                operationWithDestination.Destination?.SetManifestContext(manifest);
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Remove { get; set; } = false;

        /// <summary>
        /// Gets a value indicating whether existing files should be replaced during the operation.
        /// </summary>
        /// <remarks>The value is determined by the associated <c>Manifest</c> object. If <c>Manifest</c>
        /// is <see langword="null"/>, the default value is <see langword="false"/>.</remarks>
        public bool ReplaceExistingFiles => Manifest?.ReplaceExistingFiles ?? false;
        /// <summary>
        /// Gets a value indicating whether existing symbolic links should be replaced.
        /// </summary>
        /// <remarks>The value is determined by the associated <c>Manifest</c> object. If <c>Manifest</c>
        /// is <see langword="null"/>, the default value is <see langword="false"/>.</remarks>
        public bool ReplaceExistingSymlinks => Manifest?.ReplaceExistingSymlinks ?? false;
        public bool IgnoreMissingSource => Manifest?.IgnoreMissingSource ?? false;
        public bool IgnoreMissingDestination => Manifest?.IgnoreMissingDestination ?? false;

        public FileOperation()
        {
            Source = new VariableString();
        }

        public FileOperation(string source)
        {
            Source = new VariableString(source);
        }

        //protected void EnsurePathWritable(string path)
        //{
        //    if (!ReplaceExistingFiles && File.Exists(path))
        //        throw new FileAlreadyExistsException(this, $"Destination '{path}' already exists and will not be replaced.");
        //}

        /// <summary>
        /// Executes the operation without validating any rules
        /// </summary>
        public abstract void ExecuteWithoutRules();

        public virtual bool DeployedFileIsUnchanged()
        {
            return false; // Default implementation assumes the file is changed
        }

        public virtual bool DeployedFileExists()
        {
            if (this is IFileOperationWithDestination operationWithDestination)
            {
                return Path.Exists(operationWithDestination.Destination);
            }
            // Default implementation assumes the file does not exist
            // can't be sure Source refers to deployed file
            return false;
        }

        /// <summary>
        /// Retrieves the deployment path for the current operation. This is generally the Source or Destination path (if type implements <see cref="IFileOperationWithDestination"/>).
        /// </summary>
        /// <remarks>If the operation implements <see cref="IFileOperationWithDestination"/> and a
        /// destination is specified, the destination path is returned. Otherwise, the source path is returned as the
        /// default.</remarks>
        /// <returns>A string representing the deployment path. A blank string is returned if the path is not relevant, e.g. the operation does not deploy a file.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the operation implements <see cref="IFileOperationWithDestination"/> but the destination is null.</exception>
        public virtual string GetDeploymentPath()
        {
            if (this is IFileOperationWithDestination operationWithDestination)
            {
                if (operationWithDestination.Destination is null)
                    throw new InvalidOperationException($"Operation '{GetType().Name}' requires a destination.");
                return operationWithDestination.Destination.ToString();
            }
            return Source.ToString(); // Default to source if no destination is specified
        }

        /// <summary>
        /// Executes the operation, validating it first if rules are present
        /// </summary>
        /// <returns></returns>
        public virtual FileOperationExecutionResult Execute()
        {
            if (SkipIfSourceDoesNotExist && !FileUtils.PathExists(Source))
            {
                return FileOperationExecutionResult.Skipped;
            }

            if (this is IFileOperationWithDestination withDest)
            {
                if (FileUtils.PathExists(withDest.Destination))
                {
                    if (!ReplaceExistingFiles && (!ReplaceExistingSymlinks || !FileUtils.IsSymbolicLink(withDest.Destination)))
                    {
                        //throw new FileAlreadyExistsException(this, $"Destination '{Destination}' already exists and will not be replaced.");
                        //Log(new(this, $"Destination '{Destination}' already exists and will not be replaced.", ));
                        Log(new(this, new FileWillNotBeReplaced() { Target = RuleTarget.Destination }) { Type = LogEntryType.Info });
                        return FileOperationExecutionResult.Skipped; // Skip operation if we don't replace existing files or symlinks
                    }
                }
                else if (withDest.SkipIfDestinationDoesNotExist)
                {
                    return FileOperationExecutionResult.Skipped;
                }
            }

            // Check all ValidationRules before executing the operation
            if (!Validate())
                return FileOperationExecutionResult.Failed;

            try
            {
                ExecuteWithoutRules();
            }
            catch (Exception ex)
            {
                Log(new(this, $"File operation failed: {ex.Message}", ex));
                return FileOperationExecutionResult.Failed;
            }

            return FileOperationExecutionResult.Success;
        }

        /// <summary>
        /// Validates the operation against its rules.
        /// </summary>
        /// <returns></returns>
        public virtual bool Validate()
        {

            string? destination = null;

            if (this is IFileOperationWithDestination operationWithDestination)
            {
                if (operationWithDestination.Destination is null)
                    throw new InvalidOperationException($"Operation '{GetType().Name}' requires a destination.");

                destination = operationWithDestination.Destination;
            }

            List<ValidationRule>? rules = Rules != null ? Rules : Manifest?.DefaultRules;

            if (rules != null)
            {
                
                foreach (ValidationRule rule in rules)
                {
                    try
                    {
                        ValidationResult result = rule.Validate(Source, destination);
                        if (!result.Success)
                        {
                            if (result.Message is not null)
                                Log(new(this, rule, result.Message));
                            else
                                Log(new(this, rule));

                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(new(this, rule, ex));
                        return false;
                    }
                }
            }

            //if (Rules is null)
            //{
            //    if (Manifest is not null && Manifest.DefaultRules is not null)
            //    {
            //        foreach (ValidationRule rule in Manifest.DefaultRules)
            //        {
            //            if (!rule.Validate(Source, destination))
            //            {
            //                Log(new(this, rule));
            //                //return false;
            //                // for testing only
            //                return true;
            //            }
            //        }
            //    }

            //    return true;
            //}

            //foreach (ValidationRule rule in Rules)
            //{
            //    if (!rule.Validate(Source, destination))
            //        //return false;
            //        // for testing only
            //        return true;
            //}

            return true;
        }

        /// <summary>
        /// Logs the specified log entry to the underlying logging mechanism.
        /// </summary>
        /// <remarks>This method delegates the logging operation to the associated manifest, if one is
        /// available.</remarks>
        /// <param name="entry">The log entry to be logged.</param>
        public virtual void Log(LogEntry entry)
        {
            Manifest?.Log(entry);
        }
    }
}
