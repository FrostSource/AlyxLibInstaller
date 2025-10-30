namespace FileDeployment.Logging;

/// <summary>
/// Used for logging purposes when <see cref="DeploymentManifest.ReplaceExistingFiles"/> is false and a file operation is attempted that would replace an existing file.
/// 
/// This is not a validation rule for use in actual deployment.
/// </summary>
public class FileWillNotBeReplaced : ValidationRule
{
    protected override ValidationResult ValidateInternal(string path)
    {
        throw new NotImplementedException("FileWillNotBeReplaced is not a usable validation rule.");
    }
}
