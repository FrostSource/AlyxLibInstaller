
namespace FileDeployment.Rules
{
    [ValidationRuleAlias("Hash")]
    public class HashRule : ValidationRule, IValidationRuleWithValue
    {
        public string Value { get; set; } = string.Empty;

        protected override ValidationResult ValidateInternal(string path)
        {
            var fileHash = FileUtils.GetFileHash(path!);
            //var fileHash = "FAKEHASH";

            // remove after testing
            //Console.WriteLine($"Validating hash: {path} => {fileHash}");
            if (fileHash != Value)
                return $"File '{path}' hash '{fileHash}' does not match expected hash '{Value}'.";

            return true;
        }
    }
}
