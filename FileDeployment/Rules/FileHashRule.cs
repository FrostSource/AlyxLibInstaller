using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileHash")]
    public class FileHashRule : ValidationRule
    {
        protected override ValidationResult ValidateInternal(string path)
        {

            //TODO: How would this work? Compare target against which file? How does user specify? Always compare source against destination?
            throw new NotImplementedException();
        }
    }
}
