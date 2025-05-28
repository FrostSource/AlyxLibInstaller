
namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileDoesNotExist")]
    class FileDoesNotExistRule : ValidationRule
    {
        //public override ValidationType Type => ValidationType.FileDoesNotExist;
        protected override bool ValidateInternal(string? path)
        {
            var result = !File.Exists(path) && !Directory.Exists(path);

            // remove after testing
            Console.WriteLine($"Validating file does not exist: {path} => {result}");

            // Should directory existance return false?
            return result;
        }
    }
}
