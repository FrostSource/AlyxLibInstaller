using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Logging
{
    public interface IFileDeploymentLogger
    {

        public void Log(LogEntry entry);
    }
}
