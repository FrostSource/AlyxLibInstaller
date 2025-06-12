using FileDeployment.Rules;
using System.Text.Json.Serialization;
using System.Text.Json;
using FileDeployment.Extensions;

namespace FileDeployment.Converters
{
    public class ValidationRuleConverter : JsonConverter<ValidationRule>
    {
        public override ValidationRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string typeName;
            //string? value = null;
            //RuleTarget target = RuleTarget.Source;
            ValidationRule rule;

            if (reader.TokenType == JsonTokenType.String)
            {
                // If just a string, assume it's the type name
                typeName = reader.GetString() ?? throw new JsonException("Rule type cannot be null.");

                rule = ValidationRuleFactory.CreateRule(typeName);
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;

                // Get the 'Type' property
                if (!root.TryGetProperty("Type", options.PropertyNameCaseInsensitive, out JsonElement typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    throw new JsonException("Missing or invalid 'Type' property.");
                }

                typeName = typeElement.GetString() ?? throw new JsonException("Validation rule type cannot be null.");

                Type type = ValidationRuleFactory.GetTypeFromName(typeName);

                rule = (ValidationRule)JsonSerializer.Deserialize(root.GetRawText(), type, options)!;
            }
            else
            {
                throw new JsonException("Invalid JSON format for ValidationRule.");
            }

            // **Validation (kept inline instead of separate method)**
            if (rule is IValidationRuleWithValue ruleWithValue && string.IsNullOrWhiteSpace(ruleWithValue.Value))
            {
                throw new JsonException($"Validation rule '{ValidationRuleFactory.GetAliasFromType(rule.GetType())}' requires a 'Value' property but none was provided.");
            }

            return rule;
        }

        public override void Write(Utf8JsonWriter writer, ValidationRule value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
