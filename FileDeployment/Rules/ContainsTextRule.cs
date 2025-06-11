
namespace FileDeployment.Rules
{
    [ValidationRuleAlias("ContainsText")]
    public class ContainsTextRule : ValidationRule, IValidationRuleWithValue
    {

        public string Value { get; set; } = string.Empty;

        protected override ValidationResult ValidateInternal(string path)
        {
            // Let exception be logged
            //if (!File.Exists(path))
            //    return false;

            var fileText = File.ReadAllText(path);
            if (!fileText.Contains(Value))
                return $"File '{path}' does not contain the specified text: '{Value}'";

            return true;
        }
    }
}
