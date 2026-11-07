using AlyxLib;
using Source2HelperLibrary;

namespace AlyxLibInstallerShared.Services;

/// <summary>
/// File operations performed on an addon when installing or removing AlyxLib.
/// </summary>
public interface IAddonFileOperations
{
    AddonConfig GetInstalledConfig(LocalAddon addon);
    bool HasGitRepository(LocalAddon addon);
    void Install(LocalAddon addon, AddonConfig options);
    void RemoveForUpload(LocalAddon addon, AddonConfig options);
    void UninstallOptions(LocalAddon addon, AddonConfig options);
    void UninstallFully(LocalAddon addon, bool removeGitRepository);
}

public class AddonFileOperations(FileManager fileManager) : IAddonFileOperations
{
    public AddonConfig GetInstalledConfig(LocalAddon addon) => AlyxLibHelpers.GetAddonConfig(addon);

    // Only the addon's own .git folder is ever deleted, so a parent repository doesn't count
    public bool HasGitRepository(LocalAddon addon) => GitHelper.FolderIsRepositoryRoot(addon.ContentPath);

    public void Install(LocalAddon addon, AddonConfig options) => fileManager.InstallAlyxLib(addon, options);

    public void RemoveForUpload(LocalAddon addon, AddonConfig options) => fileManager.UninstallAlyxLibForUpload(addon, options);

    public void UninstallOptions(LocalAddon addon, AddonConfig options) => fileManager.UninstallAlyxLibOptions(addon, options);

    public void UninstallFully(LocalAddon addon, bool removeGitRepository) => fileManager.UninstallAlyxLibFully(addon, removeGitRepository);
}
