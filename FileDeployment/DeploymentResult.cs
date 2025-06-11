using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment;
public class DeploymentResult(int successfulOperations, int failedOperations)
{
    /// <summary>
    /// Gets or sets the total number of operations performed.
    /// </summary>
    public int TotalOperations => successfulOperations + failedOperations;
    /// <summary>
    /// Gets or sets the number of operations that were successful.
    /// </summary>
    public int SuccessfulOperations => successfulOperations;
    /// <summary>
    /// Gets or sets the number of operations that failed.
    /// </summary>
    public int FailedOperations => failedOperations;
}
