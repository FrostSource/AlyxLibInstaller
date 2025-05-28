using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment
{
    public interface IManifestContext
    {
        DeploymentManifest? Manifest { get; }

        void SetManifestContext(DeploymentManifest manifest);
    }
}
