using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDeployment
{
    public static class ValidationRuleFactory
    {
        public static readonly Dictionary<string, Type> _ruleTypeMap = LoadRuleTypes();

        private static Dictionary<string, Type> LoadRuleTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ValidationRule).IsAssignableFrom(type) && !type.IsAbstract)
                .ToDictionary(
                    type =>
                    {
                        var aliasAttribute = type.GetCustomAttribute<ValidationRuleAliasAttribute>();
                        return aliasAttribute?.Alias ?? type.Name; // Use alias if available, otherwise use class name
                    },
                    type => type,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        public static Type GetTypeFromName(string name)
        {
            if (!_ruleTypeMap.TryGetValue(name, out Type? type))
            {
                throw new JsonException($"Unknown validation rule type: {name}");
            }
            return type;
        }

        public static string GetAliasFromType(Type type)
        {
            var aliasAttribute = type.GetCustomAttribute<ValidationRuleAliasAttribute>();
            return aliasAttribute?.Alias ?? type.Name; // Return alias if available, otherwise return type name
        }

        public static ValidationRule CreateRule(string type, string? value = null)
        {
            if (!_ruleTypeMap.TryGetValue(type, out Type? ruleType))
            {
                throw new JsonException($"Unknown validation rule type: {type}");
            }

            var ruleInstance = (ValidationRule)Activator.CreateInstance(ruleType)!;

            // If the rule supports a value, set it
            if (ruleInstance is IValidationRuleWithValue ruleWithValue && value is not null)
            {
                ruleWithValue.Value = value;
            }

            return ruleInstance;
        }
    }
}
