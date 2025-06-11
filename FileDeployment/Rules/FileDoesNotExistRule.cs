
namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileDoesNotExist")]
    public class FileDoesNotExistRule : ValidationRule
    {
        protected override ValidationResult ValidateInternal(string path)
        {
            if (File.Exists(path))
                return $"File '{path}' exists.";
            
            if (Directory.Exists(path))
                return $"Directory '{path}' exists.";

            // remove after testing
            //Console.WriteLine($"Validating file does not exist: {path} => {result}");

            // Should directory existance return false?
            return true;
        }
    }
}
