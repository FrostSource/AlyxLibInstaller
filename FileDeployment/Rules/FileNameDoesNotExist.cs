
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileNameDoesNotExist")]
    public class FileNameDoesNotExistRule : ValidationRule, IValidationRuleWithValue
    {
        public string Value { get; set; } = string.Empty;

        public const string RegexPrefix = "rx:";

        /// <summary>
        /// Gets a value indicating whether the file name to search for is interpreted as a regular expression.
        /// If the value starts with "rx:", it is treated as a regex pattern; otherwise, it is treated as a literal file name.
        /// </summary>
        [MemberNotNullWhen(true, nameof(RegexPattern))]
        public bool NameIsRegex => Value.StartsWith(RegexPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the regular expression pattern if the value is specified as a regex.
        /// </summary>
        public string? RegexPattern
        {
            get
            {
                if (NameIsRegex)
                    return Value[RegexPrefix.Length..].Trim(); // Remove "rx:" prefix
                return null;
            }
        }

        protected override ValidationResult ValidateInternal(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(Value))
                return true;

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return true;

            Regex? regex = null;
            if (NameIsRegex)
                regex = new Regex(RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            bool MatchesName(string name) =>
                regex != null
                    ? regex.IsMatch(name)
                    : string.Equals(name, Value, StringComparison.OrdinalIgnoreCase);

            // Check files
            var matchingFile = Directory.GetFiles(directory)
                .FirstOrDefault(file => MatchesName(Path.GetFileName(file)));
            if (matchingFile != null)
                return $"File found matching {(regex != null ? $"regex {RegexPattern}" : $"name '{Value}'")}: '{matchingFile}'";

            // Check directories
            var matchingDir = Directory.GetDirectories(directory)
                .FirstOrDefault(dir => MatchesName(Path.GetFileName(dir)));
            if (matchingDir != null)
                return $"Directory found matching {(regex != null ? $"regex {RegexPattern}" : $"name '{Value}'")}: '{matchingDir}'";

            return true;
        }
    }
}
