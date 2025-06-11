using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FileDeployment
{
    public interface IFileOperationWithDestination
    {
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VariableString Destination { get; set; }

        public bool SkipIfDestinationDoesNotExist => false;
    }
}
