using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileDeployment.Converters
{
    public class VariableStringConverter : JsonConverter<VariableString>
    {
        public override VariableString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new VariableString(reader.GetString() ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, VariableString value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.RawString);
        }
    }
}
