using FileDeployment.Converters;
using System.Text.Json.Serialization;

namespace FileDeployment
{
    [JsonConverter(typeof(ValidationRuleConverter))]
    public abstract class ValidationRule : IManifestContext
    {
        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public abstract ValidationType Type { get; }

        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public string? Value { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public virtual RuleTarget Target { get; set; } = RuleTarget.Source;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        //TODO: Is manifest used in rules? Can be removed?
        public DeploymentManifest? Manifest { get; private set; }
        public void SetManifestContext(DeploymentManifest manifest)
        {
            Manifest = manifest;
        }

        public bool HasValue => this is IValidationRuleWithValue ruleWithValue && !string.IsNullOrEmpty(ruleWithValue.Value);

        public string GetTargetPath(string? source, string? destination)
        {
            return Target switch
            {
                RuleTarget.Source => source ?? throw new ArgumentNullException(nameof(source)),
                RuleTarget.Destination => destination ?? throw new ArgumentNullException(nameof(destination)),
                _ => throw new InvalidOperationException("Invalid rule target")
            };
        }

        public string GetTargetPath(FileOperation operation)
        {
            return Target switch
            {
                RuleTarget.Source => operation.Source.ToString() ?? throw new ArgumentNullException(nameof(operation.Source)),
                RuleTarget.Destination => (operation as IFileOperationWithDestination)?.Destination?.ToString() ?? throw new ArgumentNullException("Destination"),
                _ => throw new InvalidOperationException("Invalid rule target")
            };
        }

        protected abstract ValidationResult ValidateInternal(string path);

        public virtual ValidationResult Validate(string? source, string? destination)
        {
            //string path = Target switch
            //{
            //    RuleTarget.Source => source ?? throw new ArgumentNullException(nameof(source)),
            //    RuleTarget.Destination => destination ?? throw new ArgumentNullException(nameof(destination)),
            //    _ => throw new InvalidOperationException("Invalid rule target")
            //};
            string path = GetTargetPath(source, destination);

            if (this is IValidationRuleWithValue ruleWithValue && string.IsNullOrEmpty(ruleWithValue.Value))
                throw new InvalidOperationException($"Validation rule '{GetType().Name}' requires a value.");

            return ValidateInternal(path);
        }
    }
}
