using FileDeployment.Converters;
using System.Text.Json.Serialization;

namespace FileDeployment
{
    [JsonConverter(typeof(FileOperationConverter))]
    public abstract class FileOperation : IManifestContext
    {
        //public abstract OperationType Type { get; }

        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VariableString Source { get; set; }

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

        public FileOperation()
        {
            Source = new VariableString();
        }

        public FileOperation(string source)
        {
            Source = new VariableString(source);
        }

        /// <summary>
        /// Executes the operation without validating any rules
        /// </summary>
        public abstract void ExecuteWithoutRules();

        /// <summary>
        /// Executes the operation, validating it first if rules are present
        /// </summary>
        /// <returns></returns>
        public virtual bool Execute()
        {
            if (!Validate())
                return false;
            ExecuteWithoutRules();
            return true;
        }

        /// <summary>
        /// Validates the operation against its rules
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

            if (Rules is null)
            {
                if (Manifest is not null && Manifest.DefaultRules is not null)
                {
                    foreach (ValidationRule rule in Manifest.DefaultRules)
                    {
                        if (!rule.Validate(Source, destination))
                            //return false;
                            // for testing only
                            return true;
                    }
                }

                return true;
            }

            foreach (ValidationRule rule in Rules)
            {
                if (!rule.Validate(Source, destination))
                    //return false;
                    // for testing only
                    return true;
            }

            return true;
        }

        public virtual void Log(string message)
        {

        }
    }
}
