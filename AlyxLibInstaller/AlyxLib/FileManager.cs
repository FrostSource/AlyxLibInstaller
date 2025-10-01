using FileDeployment;
using LibGit2Sharp;
using Microsoft.UI.Xaml.Controls;
using Source2HelperLibrary;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AlyxLibInstaller.AlyxLib;

public class FileManager
{
    private readonly AlyxLibManager manager;

    private const string DefaultSoundEventHash = "768e1cb207576e41b92718e0559f876095618a23f7a116a829a7b5d578591eeb";
    private const string DefaultResourceManifestHash = "495d7301afadbed3eece2d16250608b4e4c9529fd3d34a8f93fbf61479c6ab13";

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

    /// <summary>
    /// Checks if a Git repository is initialized in the addon content folder.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message, InfoBarSeverity Severity) CheckGit(LocalAddon addon)
    {
        if (manager.IssueFound()) return (FileCheckResult.AlyxLibPathNotFound, "Unexpected error occured", InfoBarSeverity.Error);

        if (Repository.IsValid(addon.ContentPath))
            return (FileCheckResult.FullyInstalled, "", InfoBarSeverity.Success);

        return (FileCheckResult.NotInstalled, "", InfoBarSeverity.Success);
    }
    /// <summary>
    /// Checks for any issues with panorama related files in the addon.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message, InfoBarSeverity Severity) CheckPanoramaFiles(LocalAddon addon)
    {
        return CheckFileList(panoramaFiles, addon);
    }
    /// <summary>
    /// Checks for any issues with sound event related files in the addon.
    /// </summary>
    /// <param name="addon"></param>
    /// <returns></returns>
    public (FileCheckResult Result, string Message, InfoBarSeverity Severity) CheckSoundEventFiles(LocalAddon addon)
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
    public (FileCheckResult Result, string Message, InfoBarSeverity Severity) CheckVScriptFiles(LocalAddon addon)
    {
        return CheckFileList(vscriptFiles, addon);
    }

    public bool TryGetTemplateFile(string templatePath, out string template)
    {
        templatePath = Path.Combine("templates", templatePath);
        var fullTemplatePath = Path.Combine(manager.AlyxLibPath.FullName, templatePath);
        if (File.Exists(fullTemplatePath))
        {
            template = File.ReadAllText(fullTemplatePath);
            return true;
        }
        else
        {
            App.DebugConsoleError($"{templatePath} template file not found");
            template = "";
            return false;
        }
    }

    public bool TryGetDeploymentManifest(LocalAddon addon, AddonConfig options, out DeploymentManifest manifest, out AlyxLibFileDeploymentLogger logger)
    {
        try
        {
            manifest = FileDeployment.DeploymentManifest.LoadFromFile(
                Path.Combine(manager.AlyxLibPath.FullName, "deployment_manifest.json")
                );
        }
        catch (FileNotFoundException ex)
        {
            App.DebugConsoleError($"Deployment manifest file not found: {ex.Message}");
            manifest = null;
            logger = null;
            return false;
        }

        logger = new AlyxLibFileDeploymentLogger(addon);
        manifest.Logger = logger;
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

        App.DebugConsoleMessage($"Installing AlyxLib for {addon.Name}...");

        bool failureOccurred = false;
        DeploymentResult result;

        if (options.VScriptInstalled)
        {
            App.DebugConsoleMessage("Installing VScript files...");
            if (!manifest.TryDeployCategory("vscript", out result))
            {
                failureOccurred = true;
                App.DebugConsoleError("Failed to deploy VScript files. Check the deployment manifest for errors.");
            }

            switch (options.EditorType)
            {
                case ScriptEditor.VisualStudioCode:
                    App.DebugConsoleMessage("Installing Visual Studio Code settings...");
                    if (!manifest.TryDeployCategory("editor-vscode", out result))
                    {
                        failureOccurred = true;
                        App.DebugConsoleError("Failed to deploy Visual Studio Code settings. Check the deployment manifest for errors.");
                    }
                    break;
                case ScriptEditor.None:
                    App.DebugConsoleVerbose("User chose not to install any script editor settings");
                    break;
            }
        }

        if (options.PanoramaInstalled)
        {
            App.DebugConsoleMessage("Installing panorama files...");
            if (!manifest.TryDeployCategory("panorama", out result))
            {
                failureOccurred = true;
                App.DebugConsoleError("Failed to deploy panorama files. Check the deployment manifest for errors.");
            }
            else
            {
                // Panorama files need to be compiled
                App.DebugConsoleMessage("Compiling panorama files...");
                var panoramaFiles = manifest.GetCategoryDestinations("panorama");
                // panoramadoc.js is just for code completion
                var docFiltered = panoramaFiles.Where(f => !f.Contains("panoramadoc", StringComparison.OrdinalIgnoreCase)).ToArray();
                HLA.CompileFiles(docFiltered);
            }
        }

        if (options.SoundEventInstalled)
        {
            App.DebugConsoleMessage("Installing sound event files...");
            if (!manifest.TryDeployCategory("sounds", out result))
            {
                failureOccurred = true;
                App.DebugConsoleError("Failed to deploy sound event files. Check the deployment manifest for errors.");
            }
        }

        if (options.GitInstalled)
        {
            App.DebugConsoleMessage("Initializing Git repository...");

            try
            {
                // Check if the repository is already initialized
                if (Repository.IsValid(addon.ContentPath))
                {
                    App.DebugConsoleVerbose("Git repository already initialized");
                }
                else
                {
                    Repository.Init(addon.ContentPath);
                    App.DebugConsoleVerbose("Initialized new Git repository");
                }

                if (TryGetTemplateFile("gitignore.txt", out var gitignore))
                {
                    if (File.Exists(addon.ContentFile(".gitignore")))
                    {
                        App.DebugConsoleVerbose(".gitignore file already exists so it will not be overwritten");
                    }
                    else
                    {
                        WriteAllText(addon.ContentFile(".gitignore"), gitignore);
                        App.DebugConsoleVerbose("Created .gitignore file");
                    }
                }
                else
                {
                    failureOccurred = true;
                    App.DebugConsoleError("Gitignore template file not found");
                }
            }
            catch (Exception ex)
            {
                failureOccurred = true;
                App.DebugConsoleError($"Failed to initialize Git repository: {ex.Message}");
            }
        }

        // Post-installation tasks

        App.DebugConsoleVerbose("Saving AlyxLib config...");
        AlyxLibHelpers.SaveAddonConfig(addon, options);

        failureOccurred |= logger.HasExceptions;

        if (failureOccurred)
        {
            App.DebugConsoleError($"AlyxLib installation encountered errors. Check log for details {FileLogger.LogFilePath}");
        }
        else
        {
            App.DebugConsoleSuccess("AlyxLib installation complete!");
        }
    }

    public void UninstallAlyxLibForUpload(LocalAddon addon, AddonConfig options)
    {
        if (!TryGetDeploymentManifest(addon, options, out var manifest, out var logger)) { return; }

        App.DebugConsoleMessage($"Removing AlyxLib files in {addon.Name} for workshop upload...");

        if (!manifest.TryUndeployCategory("vscript", out var result))
        {
            App.DebugConsoleError($"AlyxLib removal encountered errors. Check log for details {FileLogger.LogFilePath}");
            return;
        }

        App.DebugConsoleMessage("HUH??");
        foreach (var panoFile in manifest.GetCategoryDestinations("panorama"))
        {
            App.DebugConsoleMessage(panoFile);
        }

        if (result.FailedOperations > 0 || result.SuccessfulOperations == 0)
        {
            App.DebugConsoleWarning($"Some files could not be removed. Check log for details {FileLogger.LogFilePath}");
            return;
        }

        App.DebugConsoleSuccess("Necessary files have been removed! You may now upload your addon to the workshop.");
    }

    public void UninstallAlyxLib(LocalAddon addon, AddonConfig options, bool removeChangedFiles = false)
    {
        if (!TryGetDeploymentManifest(addon, options, out var manifest, out var logger)) { return; }

        manifest.RemoveChangedFiles = removeChangedFiles;

        bool failureOccurred = false;

        void undeploy(bool option, string[] categories, string? name = null)
        {
            if (!option)
            {
                foreach (string category in categories)
                {
                    if (string.IsNullOrEmpty(category)) continue;

                    if (!manifest.TryUndeployCategory(category, out var result))
                    {
                        App.DebugConsoleVerboseError($"Removal of {name ?? category} encountered errors");
                        failureOccurred = true;
                        continue;
                    }

                    if (result.FailedOperations > 0 || result.SuccessfulOperations == 0)
                    {
                        failureOccurred = true;
                        continue;
                    }

                    App.DebugConsoleVerbose($"Removed {name ?? category}");
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

        undeploy(options.PanoramaInstalled, ["panorama"]);
        undeploy(options.SoundEventInstalled, ["sounds"]);
        
        if (!options.GitInstalled)
        {
            string gitFolder = Path.Combine(addon.ContentPath, ".git");

            if (Directory.Exists(gitFolder))
            {
                try
                {
                    Directory.Delete(gitFolder, true);
                    App.DebugConsoleVerbose("Removed .git folder");
                }
                catch (Exception ex)
                {
                    failureOccurred = true;
                    App.DebugConsoleVerboseError($".git folder could not be removed: {ex.Message}");
                }
            }
            else
            {
                App.DebugConsoleVerbose(".git folder not found");
            }
        }

        if (failureOccurred)
        {
            App.DebugConsoleWarning($"Some files could not be removed. Check log for details {FileLogger.LogFilePath}");
            return;
        }
    }

    public static void WriteAllText(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, contents);
    }

    private (FileCheckResult Result, string Message, InfoBarSeverity Severity) CheckFileList(SymlinkMap[] fileList, LocalAddon addon)
    {
        if (manager.IssueFound()) return (FileCheckResult.AlyxLibPathNotFound, "Unexpected error occured", InfoBarSeverity.Error);

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
                        return (FileCheckResult.Warning, $"This path already exists in your addon and cannot be used by AlyxLib: {file.To}", InfoBarSeverity.Warning);
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
            return (FileCheckResult.FullyInstalled, "", InfoBarSeverity.Success);
        else if (filesInstalled == 0)
            return (FileCheckResult.NotInstalled, "", InfoBarSeverity.Success);
        else
            // Should return message of which files not installed?
            return (FileCheckResult.PartiallyInstalled, "", InfoBarSeverity.Warning);
    }
}