using AlyxLib.Logging;
using FileDeployment;
using Source2HelperLibrary;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AlyxLib;

public class FileManager
{
    private readonly AlyxLibManager manager;

    private const string DefaultSoundEventHash = "768e1cb207576e41b92718e0559f876095618a23f7a116a829a7b5d578591eeb";
    private const string DefaultResourceManifestHash = "495d7301afadbed3eece2d16250608b4e4c9529fd3d34a8f93fbf61479c6ab13";

    public ILogger? Logger => manager.Logger;
    public bool VerboseLogging => manager.VerboseLogging;

    public static SymlinkMap[] vscriptFiles =
        [
            new(@"scripts\vscripts\alyxlib"),
            new(@"scripts\vscripts\game\gameinit.lua"),
            //new(@".vscode\alyxlib.code-snippets"),
            //new(@".vscode\vlua_snippets.code-snippets"),
            //new(@"scripts") { IsContentFile = false },
        ];

    //TODO: Compile these files automatically
    public static SymlinkMap[] panoramaFiles =
        [
            new(@"panorama\scripts\custom_game\panorama_lua.js") { IsSymbolicLink = false },
            new(@"panorama\scripts\custom_game\panoramadoc.js") { IsSymbolicLink = false },
        ];

    public FileManager(AlyxLibManager manager) => this.manager = manager;

    //TODO: These file checkers should really use FileDeployment

    /// <summary>
    /// Checks if a Git repository is initialized in the addon content folder.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message) CheckGit(LocalAddon addon)
    {
        if (manager.IssueFound()) return (FileCheckResult.AlyxLibPathNotFound, "Unexpected error occured");

        //if (Repository.IsValid(addon.ContentPath))
        if (GitHelper.FolderHasGitRepository(addon.ContentPath))
            return (FileCheckResult.FullyInstalled, "");

        return (FileCheckResult.NotInstalled, "");
    }
    /// <summary>
    /// Checks for any issues with panorama related files in the addon.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message) CheckPanoramaFiles(LocalAddon addon)
    {
        return CheckFileList(panoramaFiles, addon);
    }
    /// <summary>
    /// Checks for any issues with sound event related files in the addon.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message) CheckSoundEventFiles(LocalAddon addon)
    {
        SymlinkMap[] files = [
            new($"soundevents/{addon.Name}_soundevents.vsndevts") { IsSymbolicLink = false },
            new($"resourcemanifests/{addon.Name}_addon_resources.vrman") { IsSymbolicLink = false },
            ];
        return CheckFileList(files, addon);
    }

    /// <summary>
    /// Checks for any issues with VScript related files in the addon.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message) CheckVScriptFiles(LocalAddon addon)
    {
        return CheckFileList(vscriptFiles, addon);
    }

    private (FileCheckResult Result, string Message) CheckFileList(SymlinkMap[] fileList, LocalAddon addon)
    {
        if (manager.IssueFound()) return (FileCheckResult.AlyxLibPathNotFound, "Unexpected error occured");

        var filesInstalled = 0;

        foreach (SymlinkMap file in fileList)
        {
            var toDir = file.IsContentFile ? addon.ContentPath : addon.GamePath!;

            var toInfo = new PathInfo(Path.Combine(toDir, file.To));

            //file.SetAddon(addon);

            if (toInfo.Exists)
            {
                // Is a symlink
                if (toInfo.LinkTarget != null)
                {
                    // but does not point to AlyxLib file that it should
                    if (toInfo.LinkTarget != manager.AlyxLibPath / file.From)
                    {
                        return (FileCheckResult.Warning, $"This path already exists in your addon and cannot be used by AlyxLib: {file.To}");
                    }
                    // File points to AlyxLib correctly, check next file
                    else
                    {
                        filesInstalled++;
                        continue;
                    }
                }
            }
        }

        if (filesInstalled == fileList.Length)
            return (FileCheckResult.FullyInstalled, "");
        else if (filesInstalled == 0)
            return (FileCheckResult.NotInstalled, "");
        else
            // Should return message of which files not installed?
            return (FileCheckResult.PartiallyInstalled, "");
    }

    public bool TryGetTemplateFile(string templatePath, out string template)
    {
        if (manager.AlyxLibPath == null)
        {
            Logger?.Log("AlyxLib path not set", LogType.Error, LogSeverity.High);
            template = "";
            return false;
        }

        templatePath = Path.Combine("templates", templatePath);
        var fullTemplatePath = Path.Combine(manager.AlyxLibPath.FullName, templatePath);

        if (File.Exists(fullTemplatePath))
        {
            template = File.ReadAllText(fullTemplatePath);
            return true;
        }
        else
        {
            Logger?.Log($"{templatePath} template file not found", LogType.Error, LogSeverity.High);
            template = "";
            return false;
        }
    }

    public bool TryGetDeploymentManifest(LocalAddon addon, AddonConfig options, out DeploymentManifest manifest, out AlyxLibFileDeploymentLogger deploymentLogger)
    {
        try
        {
            manifest = FileDeployment.DeploymentManifest.LoadFromFile(
                Path.Combine(manager.AlyxLibPath.FullName, "deployment_manifest.json")
                );
        }
        catch (FileNotFoundException ex)
        {
            Logger?.Log($"Deployment manifest file not found: {ex.Message}", LogType.Error, LogSeverity.High);
            manifest = null!;
            deploymentLogger = null!;
            return false;
        }

        deploymentLogger = new AlyxLibFileDeploymentLogger(addon, Logger, VerboseLogging);
        manifest.Logger = deploymentLogger;
        manifest.ReplaceExistingSymlinks = true;

        manifest.AddVariable("AlyxLib", () => manager.AlyxLibPath.FullName);
        manifest.AddVariable("AddonContent", () => addon.ContentPath);
        manifest.AddVariable("AddonGame", () => addon.GamePath);
        manifest.AddVariable("ModName", () => options.ModFolderName);
        manifest.AddVariable("AddonFolderName", () => addon.Name);

        return true;
    }

    public void InstallAlyxLib(LocalAddon addon, AddonConfig options)
    {
        if (!TryGetDeploymentManifest(addon, options, out var manifest, out var logger)) { return; }

        AddonConfig currentConfig = AlyxLibHelpers.GetAddonConfig(addon);

        Logger?.Log($"Installing AlyxLib for {addon.Name}...");

        bool failureOccurred = false;
        DeploymentResult result;

        if (options.VScriptInstalled)
        {
            Logger?.Log("Installing VScript files...");

            if (currentConfig.ModFolderName != string.Empty)
            {
                var vscriptsPath = Path.Combine(addon.ContentPath, "scripts", "vscripts");
                if (currentConfig.ModFolderName != options.ModFolderName && Directory.Exists(Path.Combine(vscriptsPath, currentConfig.ModFolderName)))
                {
                    Logger?.Log($"Renaming mod folder to {options.ModFolderName}");
                    Directory.Move(Path.Combine(vscriptsPath, currentConfig.ModFolderName), Path.Combine(vscriptsPath, options.ModFolderName));

                    var modsInit = Path.Combine(vscriptsPath, "mods", "init");
                    Regex luaRegex = new Regex(@"^\d+\.lua$", RegexOptions.IgnoreCase);
                    if (File.Exists(Path.Combine(modsInit, addon.Name + ".lua")) || Directory.GetFiles(modsInit).Any(file => luaRegex.IsMatch(Path.GetFileName(file))))
                    {
                        Logger?.LogWarning($"Make sure '{modsInit}' files point to new mod folder!");
                    }
                }
            }

            if (!manifest.TryDeployCategory("vscript", out result))
            {
                failureOccurred = true;
                Logger?.LogError("Failed to deploy VScript files. Check the deployment manifest for errors.");
            }

            switch (options.EditorType)
            {
                case ScriptEditor.VisualStudioCode:
                    Logger?.Log("Installing Visual Studio Code settings...");
                    if (!manifest.TryDeployCategory("editor-vscode", out result))
                    {
                        failureOccurred = true;
                        Logger?.LogError("Failed to deploy Visual Studio Code settings. Check the deployment manifest for errors.");
                    }
                    break;
                case ScriptEditor.None:
                    Logger?.LogDetail("User chose not to install any script editor settings");
                    break;
            }
        }

        if (options.PanoramaInstalled)
        {
            Logger?.Log("Installing panorama files...");
            if (!manifest.TryDeployCategory("panorama", out result))
            {
                failureOccurred = true;
                Logger?.LogError("Failed to deploy panorama files. Check the deployment manifest for errors.");
            }
            else
            {
                // Panorama files need to be compiled
                Logger?.Log("Compiling panorama files...");
                var panoramaFiles = manifest.GetCategoryDestinations("panorama");
                // panoramadoc.js is just for code completion
                var docFiltered = panoramaFiles.Where(f => !f.Contains("panoramadoc", StringComparison.OrdinalIgnoreCase)).ToArray();
                HLA.CompileFiles(docFiltered);
            }
        }

        if (options.SoundEventInstalled)
        {
            Logger?.Log("Installing sound event files...");
            if (!manifest.TryDeployCategory("sounds", out result))
            {
                failureOccurred = true;
                Logger?.LogError("Failed to deploy sound event files. Check the deployment manifest for errors.");
            }
        }

        if (options.GitInstalled)
        {
            Logger?.Log("Initializing Git repository...");

            try
            {
                // If Git is not installed, exit early
                if (!GitHelper.IsGitInstalled())
                {
                    failureOccurred = true;
                    Logger?.LogError("Git is not installed");
                    goto EndGitInstall;
                }

                // Check if the repository is already initialized
                //if (Repository.IsValid(addon.ContentPath))
                if (GitHelper.FolderHasGitRepository(addon.ContentPath))
                {
                    Logger?.LogDetail("Git repository already initialized");
                }
                else
                {
                    //Repository.Init(addon.ContentPath);
                    GitHelper.InitRepository(addon.ContentPath);
                    Logger?.LogDetail("Initialized new Git repository");
                }

                if (TryGetTemplateFile("gitignore.txt", out var gitignore))
                {
                    //if (File.Exists(addon.ContentFile(".gitignore")))
                    if (!GitHelper.CreateGitIgnore(addon.ContentFile(".gitignore"), gitignore))
                    {
                        Logger?.LogDetail(".gitignore file already exists so it will not be overwritten");
                    }
                    else
                    {
                        //WriteAllText(addon.ContentFile(".gitignore"), gitignore);
                        Logger?.LogDetail("Created .gitignore file");
                    }
                }
                else
                {
                    failureOccurred = true;
                    Logger?.LogError("Gitignore template file not found");
                }
            }
            catch (Exception ex)
            {
                failureOccurred = true;
                Logger?.LogError($"Failed to initialize Git repository: {ex.Message}");
            }

        EndGitInstall:;
        }


        // Post-installation tasks

        Logger?.LogDetail("Saving AlyxLib config...");
        SaveAddonConfig(addon, options);

        failureOccurred |= logger.HasExceptions;

        if (failureOccurred)
        {
            Logger?.LogError($"AlyxLib installation encountered errors.");
        }
        else
        {
            Logger?.Log("AlyxLib installation complete!", LogType.Success);
        }
    }

    public void UninstallAlyxLibForUpload(LocalAddon addon, AddonConfig options)
    {
        if (!TryGetDeploymentManifest(addon, options, out var manifest, out var logger)) { return; }

        Logger?.Log($"Removing AlyxLib files in {addon.Name} for workshop upload...");

        int failedOperations = 0, successfulOperations = 0;

        if (!manifest.TryUndeployCategory("vscript", out var result))
        {
            Logger?.LogError($"VScript removal encountered errors.");
        }
        failedOperations += result.FailedOperations;
        successfulOperations += result.SuccessfulOperations;

        if (!manifest.TryDeployCategory("upload_removal", out result))
        {
            Logger?.LogError($"Upload removal encountered errors.");
        }
        failedOperations += result.FailedOperations;
        successfulOperations += result.SuccessfulOperations;

        if (failedOperations > 0)
        {
            Logger?.LogWarning($"Some files could not be removed.");
            return;
        }

        Logger?.Log("Necessary files have been removed! You may now upload your addon to the workshop.", LogType.Success);
    }

    /// <summary>
    /// Internal uninstall method which returns result data.
    /// </summary>
    /// <param name="manifest"></param>
    /// <param name="addon"></param>
    /// <param name="options"></param>
    /// <param name="removeChangedFiles"></param>
    /// <returns></returns>
    private (bool failureOccured, int failedOperations, int successfulOperations) UninstallAlyxLibOptions(DeploymentManifest manifest, LocalAddon addon, AddonConfig options, bool removeChangedFiles = false)
    {
        manifest.RemoveChangedFiles = removeChangedFiles;

        bool failureOccurred = false;
        int failedOperations = 0, successfulOperations = 0;

        void undeploy(bool option, string[] categories, string? name = null)
        {
            if (!option)
            {
                foreach (string category in categories)
                {
                    if (string.IsNullOrEmpty(category)) continue;

                    if (!manifest.TryUndeployCategory(category, out var result, ignoreRemoveFlags: true))
                    {
                        Logger?.LogError($"Removal of {name ?? category} encountered errors", LogSeverity.Low);
                        failureOccurred = true;
                        failedOperations += result.FailedOperations;
                        successfulOperations += result.SuccessfulOperations;
                        continue;
                    }

                    failedOperations += result.FailedOperations;
                    successfulOperations += result.SuccessfulOperations;

                    if (result.FailedOperations > 0 || result.SuccessfulOperations == 0)
                    {
                        failureOccurred = true;
                        continue;
                    }

                    Logger?.LogDetail($"Removed {name ?? category}");
                }
            }
        }


        undeploy(options.VScriptInstalled, [
            "vscript",
            options.EditorType switch
            {
                ScriptEditor.VisualStudioCode => "editor-vscode",
                _ => "",
            }]);

        if (!options.PanoramaInstalled)
        {
            undeploy(options.PanoramaInstalled, ["panorama"]);

            if (!manifest.TryDeployCategory("upload_removal", out var result))
            {
                Logger?.LogError($"Panorama removal encountered errors.");
            }
            failedOperations += result.FailedOperations;
            successfulOperations += result.SuccessfulOperations;
        }
        undeploy(options.SoundEventInstalled, ["sounds"]);

        if (!options.GitInstalled)
        {
            string gitFolder = Path.Combine(addon.ContentPath, ".git");

            if (Directory.Exists(gitFolder))
            {
                try
                {
                    Directory.Delete(gitFolder, true);
                    Logger?.LogDetail("Removed .git folder");
                }
                catch (Exception ex)
                {
                    failureOccurred = true;
                    Logger?.LogError($".git folder could not be removed: {ex.Message}", LogSeverity.Low);
                }
            }
            else
            {
                Logger?.LogDetail(".git folder not found");
            }
        }

        return (failureOccurred, failedOperations, successfulOperations);
    }

    public void UninstallAlyxLibOptions(LocalAddon addon, AddonConfig options, bool removeChangedFiles = false)
    {
        if (!TryGetDeploymentManifest(addon, options, out DeploymentManifest? manifest, out AlyxLibFileDeploymentLogger? logger)) { return; }

        (bool failureOccurred, int failedOperations, int successfulOperations) = UninstallAlyxLibOptions(manifest, addon, options, removeChangedFiles);

        //manifest.RemoveChangedFiles = removeChangedFiles;

        //bool failureOccurred = false;

        //void undeploy(bool option, string[] categories, string? name = null)
        //{
        //    if (!option)
        //    {
        //        foreach (string category in categories)
        //        {
        //            if (string.IsNullOrEmpty(category)) continue;

        //            if (!manifest.TryUndeployCategory(category, out var result, ignoreRemoveFlags: true))
        //            {
        //                Logger?.LogError($"Removal of {name ?? category} encountered errors", LogSeverity.Low);
        //                failureOccurred = true;
        //                continue;
        //            }

        //            if (result.FailedOperations > 0 || result.SuccessfulOperations == 0)
        //            {
        //                failureOccurred = true;
        //                continue;
        //            }

        //            Logger?.LogDetail($"Removed {name ?? category}");
        //        }
        //    }
        //}


        //undeploy(options.VScriptInstalled, [
        //    "vscript",
        //    options.EditorType switch
        //    {
        //        ScriptEditor.VisualStudioCode => "editor-vscode",
        //        _ => "",
        //    }]);

        //undeploy(options.PanoramaInstalled, ["panorama"]);
        //undeploy(options.SoundEventInstalled, ["sounds"]);

        //if (!options.GitInstalled)
        //{
        //    string gitFolder = Path.Combine(addon.ContentPath, ".git");

        //    if (Directory.Exists(gitFolder))
        //    {
        //        try
        //        {
        //            Directory.Delete(gitFolder, true);
        //            Logger?.LogDetail("Removed .git folder");
        //        }
        //        catch (Exception ex)
        //        {
        //            failureOccurred = true;
        //            Logger?.LogError($".git folder could not be removed: {ex.Message}", LogSeverity.Low);
        //        }
        //    }
        //    else
        //    {
        //        Logger?.LogDetail(".git folder not found");
        //    }
        //}

        SaveAddonConfig(addon, options);
        Logger?.LogDetail("Updating addon config...");

        if (failureOccurred)
        {
            Logger?.LogWarning($"Some files could not be removed.");
            return;
        }
    }

    public void UninstallAlyxLibFully(LocalAddon addon)
    {
        var installedConfig = AlyxLibHelpers.GetAddonConfig(addon);
        installedConfig.VScriptInstalled = false;
        installedConfig.PanoramaInstalled = false;
        installedConfig.SoundEventInstalled = false;
        installedConfig.GitInstalled = false;

        if (!TryGetDeploymentManifest(addon, installedConfig, out var manifest, out var logger)) { return; }

        //UninstallAlyxLibOptions(addon, installedConfig);
        (_, int failedOperations, int successfulOperations) = UninstallAlyxLibOptions(manifest, addon, installedConfig);

        DeploymentResult result = manifest.DeployCategory("upload_removal");

        failedOperations += result.FailedOperations;
        successfulOperations += result.SuccessfulOperations;

        Logger?.LogDetail("Removing addon config...");
        RemoveAddonConfig(addon);

        if (failedOperations > 0 || successfulOperations == 0)
        {
            Logger?.LogWarning($"Some files could not be removed.");
            return;
        }

        Logger?.Log($"AlyxLib has been fully removed from {addon.Name}.", LogType.Success);
    }

    /// <summary>
    /// Saves the addon config to the addon's .alyxlib folder.
    /// </summary>
    /// <param name="addon"></param>
    /// <param name="config"></param>
    public void SaveAddonConfig(LocalAddon addon, AddonConfig config)
    {
        var configPath = Path.Combine(addon.ContentPath, ".alyxlib/config.json");
        var folderPath = Path.GetDirectoryName(configPath);

        if (folderPath == null)
        {
            Logger?.LogError($"Failed to get folder path for config file: {configPath}");
            return;
        }

        // Create a hidden folder only if it doesn't exist (allows user to unhide it)
        if (!Directory.Exists(folderPath))
        {
            DirectoryInfo di = Directory.CreateDirectory(folderPath);
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, AddonConfigJsonContext.Default.AddonConfig));
    }

    public void RemoveAddonConfig(LocalAddon addon)
    {
        var configFolder = Path.Combine(addon.ContentPath, ".alyxlib");
        var configPath = Path.Combine(configFolder, "config.json");

        if (!File.Exists(configPath))
        {
            Logger?.LogError($"Failed to find config file: {configPath}");
            return;
        }

        File.Delete(configPath);
        Directory.Delete(configFolder);
    }

    private static void WriteAllText(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, contents);
    }
}