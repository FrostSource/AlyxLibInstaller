using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileExists")]
    class FileExistsRule : ValidationRule
    {
        //public override ValidationType Type => ValidationType.FileExists;
        protected override bool ValidateInternal(string? path)
        {
            if (path is null)
                return false;

            var result = File.Exists(path) || Directory.Exists(path);

            // remove after testing
            Console.WriteLine($"Validating file exists: {path} => {result}");

            // Should directory existance return true?
            return result;
        }
    }
}
