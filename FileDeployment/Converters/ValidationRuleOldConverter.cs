using FileDeployment.Rules;
using System.Text.Json.Serialization;
using System.Text.Json;
using FileDeployment.Extensions;

namespace FileDeployment.Converters
{
    public class ValidationRuleOldConverter : JsonConverter<ValidationRule>
    {
        public override ValidationRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? type = null;
            string? value = null;
            RuleTarget target = RuleTarget.Source;

            if (reader.TokenType == JsonTokenType.String)
            {
                // If just a string, assume it's the type name
                type = reader.GetString() ?? throw new JsonException("Rule type cannot be null.");
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

                type = typeElement.GetString();

                // Get the 'Value' property
                if (root.TryGetProperty("Value", options.PropertyNameCaseInsensitive, out JsonElement valueElement) && valueElement.ValueKind == JsonValueKind.String)
                {
                    value = valueElement.GetString();
                }
                //string? value = root.TryGetProperty("Value", options.PropertyNameCaseInsensitive, out JsonElement valueElement) && valueElement.ValueKind == JsonValueKind.String
                //    ? valueElement.GetString()
                //    : null;

                // Get the 'Target' property
                //RuleTarget target = RuleTarget.Source; // Default
                //if (root.TryGetProperty("Target", options.PropertyNameCaseInsensitive, out JsonElement targetElement) && targetElement.ValueKind == JsonValueKind.String)
                //{
                //    if (!Enum.TryParse(targetElement.GetString(), true, out target))
                //    {
                //        throw new JsonException($"Invalid 'Target' value: {targetElement.GetString()}");
                //    }
                //}
                if (root.TryGetProperty("Target", options.PropertyNameCaseInsensitive, out JsonElement targetElement) && targetElement.ValueKind == JsonValueKind.String)
                {
                    if (!Enum.TryParse(targetElement.GetString(), true, out target))
                    {
                        throw new JsonException($"Invalid 'Target' value: {targetElement.GetString()}");
                    }
                }
            }
            else
            {
                throw new JsonException("Invalid JSON format for ValidationRule.");
            }

            var rule = ValidationRuleFactory.CreateRule(type!, value);
            rule.Target = target; // Assign the parsed target

            // **Validation (kept inline instead of separate method)**
            if (rule is IValidationRuleWithValue ruleWithValue && string.IsNullOrWhiteSpace(ruleWithValue.Value))
            {
                throw new JsonException($"Validation rule '{type}' requires a 'Value' property but none was provided.");
            }

            return rule;

            //var rule = ValidationRuleFactory.CreateRule(type, value);
            //    rule.Target = target; // Assign the parsed target

            //    if (rule is IValidationRuleWithValue ruleWithValue && string.IsNullOrWhiteSpace(ruleWithValue.Value))
            //    {
            //        throw new JsonException($"Validation rule '{type}' requires a 'Value' property but none was provided.");
            //    }

            //    return rule;
            //}

            //throw new JsonException("Invalid JSON format for ValidationRule.");
        }

        public override void Write(Utf8JsonWriter writer, ValidationRule value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }

    ///
    /// DO NOT DELETE THIS BELOW CLASS UNLESS THE ABOVE NEW CLASS IS WORKING
    ///

    //public class ValidationRuleConverter : JsonConverter<ValidationRule>
    //{
    //    public override ValidationRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        if (reader.TokenType == JsonTokenType.String)
    //        {
    //            // Handle simple case where Type is just a string
    //            string ruleType = reader.GetString() ?? throw new JsonException("Rule type cannot be null.");
    //            return CreateRule(ruleType, null);
    //        }

    //        if (reader.TokenType == JsonTokenType.StartObject)
    //        {
    //            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
    //            JsonElement root = doc.RootElement;

    //            bool caseInsensitive = options.PropertyNameCaseInsensitive;



    //            if (!root.TryGetProperty("Type", options.PropertyNameCaseInsensitive, out JsonElement typeElement) ||
    //                typeElement.ValueKind != JsonValueKind.String)
    //            {
    //                throw new JsonException("Missing or invalid 'type' property.");
    //            }

    //            string type = typeElement.GetString()!;
    //            string? value = root.TryGetProperty("Value", options.PropertyNameCaseInsensitive, out JsonElement valueElement)
    //                && valueElement.ValueKind == JsonValueKind.String ? valueElement.GetString() : null;

    //            return CreateRule(type, value);
    //        }

    //        throw new JsonException("Invalid JSON format for ValidationRule.");
    //    }

    //    public override void Write(Utf8JsonWriter writer, ValidationRule value, JsonSerializerOptions options)
    //    {
    //        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    //    }

    //    private ValidationRule CreateRule(string type, string? value)
    //    {
    //        if (!Enum.TryParse(type, true, out ValidationType validationType))
    //            throw new JsonException($"Unknown validation rule type: {type}");

    //        return validationType switch
    //        {
    //            ValidationType.FileDoesNotExist => new FileDoesNotExistRule(),
    //            ValidationType.FileExists => new FileExistsRule(),
    //            ValidationType.Hash => value is not null ? new HashRule { Value = value }
    //                : throw new JsonException($"Validation rule '{type}' requires a 'value' property."),
    //            //ValidationType.FileHash => value is not null ? new FileHashRule { Value = value }
    //            //    : throw new JsonException($"Validation rule '{type}' requires a 'value' property."),
    //            _ => throw new JsonException($"Unknown validation rule type: {type}")
    //        };
    //    }

    //    //private Type GetRuleType(ValidationType type) => type switch
    //    //{
    //    //    ValidationType.Hash => typeof(HashRule),
    //    //    ValidationType.FileHash => typeof(FileHashRule),
    //    //    _ => throw new JsonException($"Unsupported validation rule type: {type}")
    //    //};
    //}
}
