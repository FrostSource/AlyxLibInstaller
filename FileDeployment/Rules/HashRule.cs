
namespace FileDeployment.Rules
{
    [ValidationRuleAlias("Hash")]
    class HashRule : ValidationRule, IValidationRuleWithValue
    {
        //public override ValidationType Type => ValidationType.Hash;

        public string Value { get; set; } = string.Empty;

        protected override bool ValidateInternal(string? path)
        {
            //var fileHash = FileUtils.GetFileHash(path);
            var fileHash = "FAKEHASH";

            // remove after testing
            Console.WriteLine($"Validating hash: {path} => {fileHash}");
            
            return fileHash == Value;
        }
    }
}
