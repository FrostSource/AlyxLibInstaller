using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDeployment.Converters
{
    public class VariableStringListConverter : JsonConverter<List<VariableString>>
    {
        public override List<VariableString> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return [reader.GetString()!];
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                return JsonSerializer.Deserialize<List<VariableString>>(ref reader, options) ?? [];
            }
            return [];
        }

        public override void Write(Utf8JsonWriter writer, List<VariableString> value, JsonSerializerOptions options)
        {
            if (value.Count == 0)
            {
                return; // Don't write anything if empty
            }

            if (value.Count == 1)
            {
                writer.WriteStringValue(value[0]);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
