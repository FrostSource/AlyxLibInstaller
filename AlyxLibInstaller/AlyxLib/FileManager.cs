using FileDeployment;
using LibGit2Sharp;
using Microsoft.UI.Xaml.Controls;
using Source2HelperLibrary;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
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
                    WriteAllText(addon.ContentFile(".gitignore"), gitignore);
                    App.DebugConsoleVerbose("Created .gitignore file");
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

    public void InstallAlyxLibOld(LocalAddon addon, AddonConfig options)
    {
        if (manager.IssueFound()) return;

        void installFiles(SymlinkMap[] symlinkMaps)
        {
            foreach (SymlinkMap file in symlinkMaps)
            {
                var toDir = Path.GetDirectoryName(file.GetFullPath(addon));

                if (!file.IsContentFile && !Directory.Exists(addon.GamePath))
                {
                    App.DebugConsoleError($"Game path not found for {addon.Name}, cannot create symlink!");
                    continue;
                }

                var toFullPath = file.GetFullPath(addon);
                var fromFullPath = Path.Combine(manager.AlyxLibPath.FullName, file.From);
                if (toDir != null)
                {
                    Directory.CreateDirectory(toDir);
                    if (file.IsSymbolicLink)
                    {

                        if (Directory.Exists(toFullPath))
                        {
                            Directory.Delete(toFullPath);
                        }
                        else if (File.Exists(toFullPath))
                        {
                            File.Delete(toFullPath);
                        }

                        App.DebugConsoleVerbose(
                            //$"Creating symbolic link from {fromFullPath} to {toFullPath}",
                            $"Creating symbolic link from {manager.AlyxLibPath.Name}{Path.DirectorySeparatorChar}{file.From} to {addon.Name}{Path.DirectorySeparatorChar}{file.To}"
                            );

                        // Check if the AlyxLib path is directory or file
                        if (Directory.Exists(fromFullPath))
                            Directory.CreateSymbolicLink(toFullPath, fromFullPath);
                        else
                            File.CreateSymbolicLink(toFullPath, fromFullPath);
                    }
                    else
                    {
                        if (File.Exists(toFullPath))
                        {
                            App.DebugConsoleVerbose($"Deleting file {toFullPath}");
                            File.Delete(toFullPath);
                        }

                        App.DebugConsoleVerbose(
                            //$"Copying file from {fromFullPath} to {toFullPath}",
                            $"Copying file from {manager.AlyxLibPath.Name}{Path.DirectorySeparatorChar}{file.From} to {addon.Name}{Path.DirectorySeparatorChar}{file.To}"
                            );

                        File.Copy(fromFullPath, toFullPath);
                    }
                }
            }
        }


        AddonConfig currentAddonConfig = AlyxLibHelpers.GetAddonConfig(addon);

        string modFolderName = options.ModFolderName ?? addon.Name;

        App.DebugConsoleMessage($"Installing AlyxLib for {addon.Name}...");

        if (options.VScriptInstalled)
        {
            App.DebugConsoleMessage("Installing VScript files...");
            installFiles(vscriptFiles);

            // Create scripts symlink in game path
            if (!Directory.Exists(addon.GamePath))
            {
                App.DebugConsoleError($"Game path not found for {addon.Name}, cannot create script symlink!");
            }
            else if (Directory.Exists(addon.GameFile("scripts")))
            {
                App.DebugConsoleVerboseWarning("Scripts folder already exists in addon game path, cannot create symlink");
            }
            else
            {
                Directory.CreateSymbolicLink(addon.GameFile("scripts"), addon.ContentFile("scripts"));
                App.DebugConsoleVerbose("Created scripts symlink in addon game path");
            }

            // Create addon init file

            if (!string.IsNullOrEmpty(currentAddonConfig.ModFolderName))
            {
                // Rename mod folder if name has changed
                if (currentAddonConfig.ModFolderName != modFolderName)
                {
                    if (Directory.Exists(addon.ContentFile($"scripts/vscripts/{currentAddonConfig.ModFolderName}")))
                    {
                        Directory.Move(addon.ContentFile($"scripts/vscripts/{currentAddonConfig.ModFolderName}"), addon.ContentFile($"scripts/vscripts/{modFolderName}"));
                        App.DebugConsoleVerbose($"Renamed mod folder from {currentAddonConfig.ModFolderName} to {modFolderName}");
                    }
                }
            }

            if (TryGetTemplateFile("script_init_main.txt", out var mainTemplate))
            {
                if (!File.Exists(addon.ContentFile($"scripts/vscripts/{modFolderName}/init.lua")))
                {
                    WriteAllText(addon.ContentFile($"scripts/vscripts/{modFolderName}/init.lua"), mainTemplate);
                    App.DebugConsoleVerbose($"Created {modFolderName}\\init.lua file");
                }
                else
                {
                    App.DebugConsoleVerbose("Addon init file already exists and will not be replaced");
                }
            }

            // Create SIS mod/init files
            //Directory.CreateDirectory(addon.ContentFile($"scripts/vscripts/mods/init"));

            if (TryGetTemplateFile("script_init_local.txt", out var localTemplate))
            {
                // Create a local init file for the addon
                WriteAllText(addon.ContentFile($"scripts/vscripts/mods/init/{addon.Name}.lua"), string.Format(localTemplate, modFolderName));
                // Delete old local init file if it exists
                var oldLocalInitPath = addon.ContentFile($"scripts/vscripts/mods/init/{currentAddonConfig.ModFolderName}.lua");
                if (File.Exists(oldLocalInitPath))
                {
                    // Make sure the file is unchanged before deleting it
                    var fileHash = Utils.GetFileHash(oldLocalInitPath);
                    var shouldBeHash = Utils.GetStringHash(string.Format(localTemplate, currentAddonConfig.ModFolderName));
                    if (fileHash == shouldBeHash)
                    {
                        File.Delete(oldLocalInitPath);
                        App.DebugConsoleVerbose($"Deleted old local mod/init file {addon.GetContentFileRelativePath(oldLocalInitPath)}");
                    }
                    else
                    {
                        App.DebugConsoleInfo(
                            $"{addon.GetContentFileRelativePath(oldLocalInitPath)} has been modified and will not be deleted",
                            $"{oldLocalInitPath} has been modified and will not be deleted"
                            );
                    }
                }
            }

            if (TryGetTemplateFile("script_init_workshop.txt", out var workshopTemplate))
            {
                var workshopFileExists = false;
                var workshopFile = "";
                // Check all files in mod/init folder for existing workshop file (any 10 digits .lua)
                var luaFiles = Directory.GetFiles(addon.ContentFile("scripts/vscripts/mods/init"), "*.lua");
                foreach (var file in luaFiles)
                    // Filename can have any digits 0-9, e.g 2958361340.lua
                    if (Path.GetFileNameWithoutExtension(file).All(char.IsDigit))
                    {
                        workshopFile = file;
                        workshopFileExists = true;
                        break;
                    }

                // Create a workshop init file for the addon
                if (!workshopFileExists)
                {
                    WriteAllText(addon.ContentFile($"scripts/vscripts/mods/init/0000000000.lua"), string.Format(workshopTemplate, modFolderName));
                }
                else
                {
                    App.DebugConsoleInfo(
                        "Possible workshop init file already exists so a new one will not be created",
                        $"Possible workshop init file already exists so a new one will not be created -> {workshopFile}"
                        );
                }
            }

            // Copy vscode settings
            switch (options.EditorType)
            {
                case ScriptEditor.VisualStudioCode:
                    // Create settings.json file
                    if (TryGetTemplateFile("vscode_settings.txt", out var vscodeSettings))
                    {
                        if (!File.Exists(addon.ContentFile(".vscode/settings.json")))
                        {
                            // Create .vscode folder if it doesn't exist
                            WriteAllText(addon.ContentFile(".vscode/settings.json"), vscodeSettings);
                            App.DebugConsoleVerbose("Created .vscode/settings.json file");
                        }
                        else
                        {
                            App.DebugConsoleInfo(".vscode/settings.json file already exists and will not be replaced\nMake sure to enable 'Half Life Alyx VScript API' in the Lua Addon Manager");
                        }
                    }

                    // Symlink snippets
                    installFiles([
                        new(@".vscode\alyxlib.code-snippets"),
                        new(@".vscode\vlua_snippets.code-snippets")
                    ]);

                    break;

                case ScriptEditor.None:
                    App.DebugConsoleVerbose("User chose not to install any script editor settings");
                    break;
            }

        }

        if (options.SoundEventInstalled)
        {
            App.DebugConsoleMessage("Installing sound event files...");
            var soundEventTemplatePath = Path.Combine(manager.AlyxLibPath.FullName, "templates/soundevents.txt");
            if (File.Exists(soundEventTemplatePath))
            {
                var soundEventTemplate = File.ReadAllText(soundEventTemplatePath);
                // If soundevent file is default name and contents it can be replaced
                if (File.Exists(addon.ContentFile("soundevents/addon_template_soundevents.vsndevts")))
                {
                    // Make sure the file is the default sound event file before replacing it
                    var hash = Utils.GetFileHash(addon.ContentFile("soundevents/addon_template_soundevents.vsndevts"));
                    //App.DebugConsoleError(GetFileHash(@"C:\Program Files (x86)\Steam\steamapps\common\Half-Life Alyx\content\hlvr_addons\addon_template\soundevents\addon_template_soundevents.vsndevts"));
                    //App.DebugConsoleError(GetFileHash(@"C:\Program Files (x86)\Steam\steamapps\common\Half-Life Alyx\content\hlvr_addons\addon_template\resourcemanifests\addon_template_addon_resources.vrman"));
                    if (hash == DefaultSoundEventHash)
                    {
                        File.Delete(addon.ContentFile("soundevents/addon_template_soundevents.vsndevts"));
                        WriteAllText(addon.ContentFile($"soundevents/{addon.Name}_soundevents.vsndevts"), soundEventTemplate);
                        App.DebugConsoleVerbose("Default sound event file replaced with template file");
                    }
                    else
                    {
                        App.DebugConsoleWarning("Sound event file is not the default file so it will not be replaced");
                        App.DebugConsoleWarning(hash);
                    }
                }
                else
                {    // Instead check if a sound event file for this addon already exists
                    if (!File.Exists(addon.ContentFile($"soundevents/{addon.Name}_soundevents.vsndevts")))
                    {
                        WriteAllText(addon.ContentFile($"soundevents/{addon.Name}_soundevents.vsndevts"), soundEventTemplate);
                        App.DebugConsoleVerbose("Sound event file created for addon");
                    }
                    else
                    {
                        App.DebugConsoleVerboseWarning("Sound event file already exists for this addon and will not be replaced");
                    }
                }

                // Resource manifest only needs to be updated if the sound event file was replaced

                var resourceManifestTemplatePath = Path.Combine(manager.AlyxLibPath.FullName, "templates/resource_manifest.txt");
                if (File.Exists(resourceManifestTemplatePath))
                {
                    // copy the template to the addon folder using the addon name and format the template to reference the addon sound event
                    var resourceManifestTemplate = File.ReadAllText(resourceManifestTemplatePath);
                    var resourceManifest = string.Format(resourceManifestTemplate, addon.Name);
                    if (File.Exists(addon.ContentFile("resourcemanifests/addon_template_addon_resources.vrman")))
                    {
                        // Make sure the file is the default resource manifest file before replacing it
                        var hash = Utils.GetFileHash(addon.ContentFile("resourcemanifests/addon_template_addon_resources.vrman"));
                        if (hash == DefaultResourceManifestHash)
                        {
                            File.Delete(addon.ContentFile("resourcemanifests/addon_template_addon_resources.vrman"));
                            WriteAllText(addon.ContentFile($"resourcemanifests/{addon.Name}_addon_resources.vrman"), resourceManifest);
                            App.DebugConsoleVerbose("Default resource manifest file replaced with template file");
                        }
                        else
                        {
                            App.DebugConsoleWarning("Resource manifest file is not the default file so it will not be replaced");
                        }
                    }
                    else
                    {
                        // Instead check if a resource manifest file for this addon already exists
                        if (!File.Exists(addon.ContentFile($"resourcemanifests/{addon.Name}_addon_resources.vrman")))
                        {
                            WriteAllText(addon.ContentFile($"resourcemanifests/{addon.Name}_addon_resources.vrman"), resourceManifest);
                            App.DebugConsoleVerbose("Resource manifest file created for addon");
                        }
                        else
                        {
                            App.DebugConsoleVerboseWarning("Resource manifest file already exists for this addon and will not be replaced");
                        }
                    }
                }
                else
                {
                    App.DebugConsoleError(
                        "Resource manifest template file not found",
                        $"Resource manifest template file not found at {resourceManifestTemplatePath}"
                        );
                }
            }
            else
            {
                App.DebugConsoleError(
                    "Sound event template file not found",
                    $"Sound event template file not found at {soundEventTemplatePath}"
                    );
            }
        }

        if (options.PanoramaInstalled)
        {
            App.DebugConsoleMessage("Installing panorama files...");
            installFiles(panoramaFiles);
        }

        if (options.GitInstalled)
        {
            App.DebugConsoleMessage("Initializing Git repository...");
            Repository.Init(addon.ContentPath);

            if (TryGetTemplateFile("gitignore.txt", out var gitignore))
            {
                WriteAllText(addon.ContentFile(".gitignore"), gitignore);
                App.DebugConsoleVerbose("Created .gitignore file");
            }
            else
            {
                App.DebugConsoleError("Gitignore template file not found");
            }
        }

        App.DebugConsoleVerbose("Saving AlyxLib config...");
        AlyxLibHelpers.SaveAddonConfig(addon, options);
        //AlyxLibManager.SaveAddonConfig(addon, new()
        //{
        //    VScriptInstalled = installVScript,
        //    SoundEventInstalled = installSoundEvent,
        //    PanoramaInstalled = installPanorama,
        //    GitInstalled = installGit,
        //    ModFolderName = modFolderName,
        //    //UseAlyxLibVSCodeSettings = installVSCodeSnippets,
        //    Version = manager.AlyxLibVersion
        //});

        App.DebugConsoleSuccess("AlyxLib installation complete!");
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

        if (result.FailedOperations > 0 || result.SuccessfulOperations == 0)
        {
            App.DebugConsoleWarning($"Some files could not be removed. Check log for details {FileLogger.LogFilePath}");
            return;
        }

        App.DebugConsoleSuccess("Necessary files have been removed! You may now upload your addon to the workshop.");
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