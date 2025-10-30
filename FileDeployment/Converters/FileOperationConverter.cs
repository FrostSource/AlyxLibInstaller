using FileDeployment.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileDeployment.Converters
{
    public class FileOperationConverter : JsonConverter<FileOperation>
    {
        public override FileOperation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Invalid JSON format for FileOperation.");
            }

            using var doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("Type", options.PropertyNameCaseInsensitive, out JsonElement typeElement) || typeElement.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing 'type' property in file operation");

            string typeName = typeElement.GetString() ?? throw new JsonException("File operation type cannot be null.");

            var type = FileOperationFactory.GetTypeFromName(typeName);

            var operation = (FileOperation)JsonSerializer.Deserialize(root.GetRawText(), type, options)!;

            if (operation is IFileOperationWithDestination operationWithDestination && string.IsNullOrEmpty(operationWithDestination.Destination))
            {
                throw new JsonException($"Operation '{typeName}' requires a destination.");
            }

            return operation;
        }

        public override void Write(Utf8JsonWriter writer, FileOperation value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

}
