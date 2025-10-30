namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileExists")]
    public class FileExistsRule : ValidationRule
    {
        protected override ValidationResult ValidateInternal(string path)
        {
            if (!File.Exists(path))
                return $"File '{path}' does not exist.";
            if (!Directory.Exists(path))
                return $"Directory '{path}' does not exist.";

            // remove after testing
            //Console.WriteLine($"Validating file exists: {path} => {result}");

            // Should directory existance return true?
            return true;
        }
    }
}
