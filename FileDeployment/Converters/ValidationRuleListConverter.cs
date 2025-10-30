using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileDeployment.Converters
{
    public class ValidationRuleListConverter : JsonConverter<List<ValidationRule>?>
    {
        private readonly ValidationRuleConverter _ruleConverter = new();

        public override List<ValidationRule>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.StartObject)
            {
                var singleRule = _ruleConverter.Read(ref reader, typeof(ValidationRule), options);
                return new List<ValidationRule> { singleRule };
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                return JsonSerializer.Deserialize<List<ValidationRule>>(ref reader, options);
            }

            throw new JsonException("Unexpected JSON format for ValidationRule list.");
        }

        public override void Write(Utf8JsonWriter writer, List<ValidationRule>? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Count == 1)
            {
                _ruleConverter.Write(writer, value[0], options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }

    ///
    /// DO NOT DELETE THIS BELOW CLASS UNLESS THE ABOVE NEW CLASS IS WORKING
    ///

    //public class ValidationRuleListConverter : JsonConverter<List<ValidationRule>?>
    //{
    //    private readonly ValidationRuleConverter _ruleConverter = new();
    //    public override List<ValidationRule>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        if (reader.TokenType == JsonTokenType.Null)
    //            return null;

    //        if (reader.TokenType == JsonTokenType.String)
    //        {
    //            string ruleType = reader.GetString()!;
    //            return new List<ValidationRule> { _ruleConverter.Read(ref reader, typeof(ValidationRule), options) };
    //        }

    //        if (reader.TokenType == JsonTokenType.StartObject)
    //        {
    //            var singleRule = JsonSerializer.Deserialize<ValidationRule>(ref reader, options);
    //            return singleRule != null ? new List<ValidationRule> { singleRule } : null;
    //        }

    //        if (reader.TokenType == JsonTokenType.StartArray)
    //        {
    //            return JsonSerializer.Deserialize<List<ValidationRule>>(ref reader, options);
    //        }

    //        throw new JsonException("Unexpected JSON format for Rules.");
    //    }

    //    public override void Write(Utf8JsonWriter writer, List<ValidationRule>? value, JsonSerializerOptions options)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNullValue();
    //            return;
    //        }

    //        if (value.Count == 1)
    //        {
    //            var singleRule = value[0];

    //            if (singleRule is FileDoesNotExistRule || singleRule is FileExistsRule)
    //            {
    //                // If it's a simple rule that doesn't need extra properties, write as a string
    //                //writer.WriteStringValue(singleRule.Type.ToString());
    //                return;
    //            }

    //            // Otherwise, write as an object
    //            JsonSerializer.Serialize(writer, singleRule, singleRule.GetType(), options);
    //            return;
    //        }

    //        // Write full list if multiple rules exist
    //        JsonSerializer.Serialize(writer, value, options); JsonSerializer.Serialize(writer, value, options);
    //    }
    //}
}
