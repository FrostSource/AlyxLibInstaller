
namespace FileDeployment.Rules
{
    [ValidationRuleAlias("ContainsText")]
    class ContainsTextRule : ValidationRule, IValidationRuleWithValue
    {

        public string Value { get; set; } = string.Empty;

        protected override bool ValidateInternal(string? path)
        {
            if (!File.Exists(path))
                return false;

            var fileText = File.ReadAllText(path);
            return fileText.Contains(Value);
        }
    }
}
