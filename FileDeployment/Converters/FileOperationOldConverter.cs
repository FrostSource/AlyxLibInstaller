using System.Text.Json.Serialization;
using System.Text.Json;
using FileDeployment.Extensions;
using FileDeployment.Operations;
using System.Reflection;

namespace FileDeployment.Converters
{
    public class FileOperationOldConverter : JsonConverter<FileOperation>
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

            var type = typeElement.GetString();

            if (!root.TryGetProperty("Source", options.PropertyNameCaseInsensitive, out JsonElement sourceElement) || sourceElement.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing 'source' property in file operation");

            var source = sourceElement.GetString();

            string? destination = null;

            if (root.TryGetProperty("Destination", options.PropertyNameCaseInsensitive, out JsonElement destinationElement) && destinationElement.ValueKind == JsonValueKind.String)
            {
                destination = destinationElement.GetString();
            }

            // get rules
            List<ValidationRule>? rules = null;
            if (root.TryGetProperty("Rules", options.PropertyNameCaseInsensitive, out JsonElement rulesElement) && rulesElement.ValueKind == JsonValueKind.Array)
            {
                rules = new List<ValidationRule>();
                foreach (var ruleElement in rulesElement.EnumerateArray())
                {
                    rules.Add(JsonSerializer.Deserialize<ValidationRule>(ruleElement.GetRawText(), options));
                }
            }

            //var typeString = typeElement.GetString();
            //if (!Enum.TryParse(typeString, true, out OperationType operationType))
            //    throw new JsonException($"Unknown operation type: {typeString}");

            //return operationType switch
            //{
            //    OperationType.Copy => JsonSerializer.Deserialize<CopyFileOperation>(root.GetRawText(), options),
            //    OperationType.Delete => JsonSerializer.Deserialize<DeleteFileOperation>(root.GetRawText(), options),
            //    OperationType.Symlink => JsonSerializer.Deserialize<SymlinkFileOperation>(root.GetRawText(), options),
            //    OperationType.Template => JsonSerializer.Deserialize<TemplateFileOperation>(root.GetRawText(), options),
            //    _ => throw new JsonException($"Unsupported operation type: {typeString}")
            //};

            var operation = FileOperationFactory.CreateFileOperation(type!, source, destination, rules);

            // Reflect and handle custom properties for subclasses
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.Equals("Type", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("Source", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("Destination", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("Rules", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Skip known properties handled already
                }

                // Handle dynamic properties by setting them using reflection (case insensitive)
                var prop = operation.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));

                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        ////var value = JsonSerializer.Deserialize(property.Value.GetRawText(), prop.PropertyType, options);
                        //var converterOptions = new JsonSerializerOptions(options)
                        //{
                        //    Converters = options.Converters.ToList() // Clone the existing converters
                        //};

                        //// Now we add the appropriate converter dynamically, if needed
                        //var value = JsonSerializer.Deserialize(property.Value.GetRawText(), prop.PropertyType, converterOptions);

                        //prop.SetValue(operation, value);

                        
                    }
                    catch (Exception ex)
                    {
                        throw new JsonException($"Error deserializing property '{property.Name}' to type '{prop.PropertyType.Name}'.", ex);
                    }
                }
            }

            if (operation is IFileOperationWithDestination operationWithDestination && string.IsNullOrEmpty(operationWithDestination.Destination))
            {
                throw new JsonException($"Operation '{type}' requires a destination.");
            }

            return operation;
        }

        public override void Write(Utf8JsonWriter writer, FileOperation value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

}
