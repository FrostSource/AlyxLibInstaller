using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Rules
{
    [ValidationRuleAlias("FileHash")]
    class FileHashRule : ValidationRule
    {
        //public override ValidationType Type => ValidationType.FileHash;
        protected override bool ValidateInternal(string? path)
        {

            //TODO: How would this work? Compare target against which file? How does user specify? Always compare source against destination?
            throw new NotImplementedException();
        }
    }
}
