using System.Reflection;
using System.Text.Json;

namespace FileDeployment
{
    public static class FileOperationFactory
    {
        private static readonly Dictionary<string, Type> _operationTypeMap = LoadOperationTypes();

        private static Dictionary<string, Type> LoadOperationTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(FileOperation).IsAssignableFrom(type) && !type.IsAbstract)
                .ToDictionary(
                    type =>
                    {
                        var aliasAttribute = type.GetCustomAttribute<FileOperationAliasAttribute>();
                        return aliasAttribute?.Alias ?? type.Name; // Use alias if available, otherwise use class name
                    },
                    type => type,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        public static string[] Aliases => [.. _operationTypeMap.Keys];

        public static Type GetTypeFromName(string name)
        {
            if (!_operationTypeMap.TryGetValue(name, out Type? type))
            {
                throw new JsonException($"Unknown file operation type: {name}");
            }
            return type;
        }

        public static FileOperation CreateFileOperation(string type, string? source, string? destination, List<ValidationRule>? rules)
        {
            if (!_operationTypeMap.TryGetValue(type, out Type? operationType))
            {
                throw new JsonException($"Unknown file operation type: {type}");
            }

            var operationInstance = (FileOperation)Activator.CreateInstance(operationType)!;

            // If the operation requires a destination, set it
            if (operationInstance is IFileOperationWithDestination operationWithDestination && destination is not null)
            {
                operationWithDestination.Destination = destination;
            }

            if (rules is not null)
            {
                operationInstance.Rules = rules;
            }

            return operationInstance;
        }
    }
}
