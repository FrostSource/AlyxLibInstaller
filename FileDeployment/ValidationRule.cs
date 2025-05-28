using System.Text.Json.Serialization;
using FileDeployment.Converters;

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

        public DeploymentManifest? Manifest { get; private set; }
        public void SetManifestContext(DeploymentManifest manifest)
        {
            Manifest = manifest;
        }

        protected abstract bool ValidateInternal(string? path);

        public virtual bool Validate(string? source, string? destination)
        {
            string path = Target switch
            {
                RuleTarget.Source => source ?? throw new ArgumentNullException(nameof(source)),
                RuleTarget.Destination => destination ?? throw new ArgumentNullException(nameof(destination)),
                _ => throw new InvalidOperationException("Invalid rule target")
            };

            if (this is IValidationRuleWithValue ruleWithValue && string.IsNullOrEmpty(ruleWithValue.Value))
                throw new InvalidOperationException($"Validation rule '{GetType().Name}' requires a value.");

            return ValidateInternal(path);
        }
    }
}
