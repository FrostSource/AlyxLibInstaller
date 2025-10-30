namespace FileDeployment
{
    public interface IManifestContext
    {
        DeploymentManifest? Manifest { get; }

        void SetManifestContext(DeploymentManifest manifest);
    }
}
