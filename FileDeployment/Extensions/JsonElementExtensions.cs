using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileDeployment.Extensions
{
    public static class JsonElementExtensions
    {
        public static bool TryGetProperty(this JsonElement root, string propertyName, bool caseInsensitive, out JsonElement propertyValue)
        {
            if (!caseInsensitive)
            {
                // Use the built-in TryGetProperty for exact match
                return root.TryGetProperty(propertyName, out propertyValue);
            }

            // Case-insensitive search
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = property.Value;
                    return true;
                }
            }

            propertyValue = default;
            return false;
        }

        public static bool TryGetPropertyInsensitive(this JsonElement root, string propertyName, out JsonElement propertyValue)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = property.Value;
                    return true;
                }
            }

            propertyValue = default;
            return false;
        }
    }
}
