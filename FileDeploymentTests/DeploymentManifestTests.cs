using FileDeployment.Operations;

namespace FileDeployment.Tests;

[TestClass()]
public class DeploymentManifestTests
{    private const string SampleJson = @"
    {

      ""Categories"": {

        ""vscript"": [
          {
            ""type"": ""symlink"",
            ""description"": ""AlyxLib library symlink"",
            ""source"": ""{AlyxLib}/scripts/vscripts/alyxlib"",
            ""destination"": ""{AddonContent}/scripts/vscripts/alyxlib""
          },
          {
            ""type"": ""symlink"",
            ""description"": ""gameinit.lua symlink"",
            ""source"": ""{AlyxLib}/scripts/vscripts/game/gameinit.lua"",
            ""destination"": ""{AddonContent}/scripts/vscripts/game/gameinit.lua""
          },
          {
            ""type"": ""copy"",
            ""description"": ""Addon main init.lua file"",
            ""source"": ""{AlyxLib}/templates/script_init_main.txt"",
            ""destination"": ""{AddonContent}/scripts/vscripts/{ModName}/init.lua""
          },
          {
            ""type"": ""template"",
            ""description"": ""Workshop mod init file"",
            ""source"": ""{AlyxLib}/templates/script_init_workshop.txt"",
            ""destination"": ""{AddonContent}/scripts/vscripts/mods/init/0000000000.lua"",
            ""replacements"": ""{ModName}"",
            ""rules"": [
              {
                ""type"": ""FileNameDoesNotExist"",
                ""target"": ""destination"",
                ""value"": ""rx:^\\d+\\.lua$"",
                ""description"": ""Possible workshop init file already exists so a new one will not be created.""
              }
            ]
          },
          {
            ""type"": ""template"",
            ""description"": ""Local mod init file"",
            ""source"": ""{AlyxLib}/templates/script_init_local.txt"",
            ""destination"": ""{AddonContent}/scripts/vscripts/mods/init/{AddonFolderName}.lua"",
            ""replacements"": ""{ModName}""
          },
          {
            ""type"": ""symlink"",
            ""description"": ""Scripts symlink in addon game folder"",
            ""source"": ""{AddonContent}/scripts"",
            ""destination"": ""{AddonGame}/scripts""
          }
        ],

        ""editor-vscode"": [
          {
            ""type"": ""symlink"",
            ""description"": ""AlyxLib snippets symlink for VSCode"",
            ""source"": ""{AlyxLib}/.vscode/alyxlib.code-snippets"",
            ""destination"": ""{AddonContent}/.vscode/alyxlib.code-snippets"",
          },
          {
            ""type"": ""symlink"",
            ""description"": ""VScript snippets symlink for VSCode"",
            ""source"": ""{AlyxLib}/.vscode/vlua_snippets.code-snippets"",
            ""destination"": ""{AddonContent}/.vscode/vlua_snippets.code-snippets""
          },
          {
            ""type"": ""copy"",
            ""description"": ""AlyxLib VSCode settings file"",
            ""source"": ""{AlyxLib}/templates/vscode_settings.txt"",
            ""destination"": ""{AddonContent}/.vscode/settings.json"",
            ""rules"": [
              {
                ""type"": ""FileDoesNotExist"",
                ""target"": ""destination"",
                ""description"": ""VSCode settings file already exists, so it will not be replaced""
              }
            ]
          }
        ],

        ""panorama"": [
          {
            ""type"": ""copy"",
            ""source"": ""{AlyxLib}/panorama/scripts/custom_game/panorama_lua.js"",
            ""destination"": ""{AddonContent}/panorama/scripts/custom_game/panorama_lua.js""
          },
          {
            ""type"": ""copy"",
            ""source"": ""{AlyxLib}/panorama/scripts/custom_game/panoramadoc.js"",
            ""destination"": ""{AddonContent}/panorama/scripts/custom_game/panoramadoc.js""
          }
        ],

        ""sounds"": [
          {
            ""type"": ""copy"",
            ""description"": ""Addon soundevents file"",
            ""source"": ""{AlyxLib}/templates/soundevents.txt"",
            ""destination"": ""{AddonContent}/soundevents/{AddonFolderName}_soundevents.vsndevts""
          },
          {
            ""type"": ""delete"",
            ""description"": ""Delete default soundevents file"",
            ""source"": ""{AddonContent}/soundevents/addon_template_soundevents.vsndevts"",
            ""rules"": [
              {
                ""type"": ""Hash"",
                ""value"": ""768e1cb207576e41b92718e0559f876095618a23f7a116a829a7b5d578591eeb"",
                ""description"": ""Default soundevents file has been modified, so it won't be deleted""
              }
            ]
          },

          {
            ""type"": ""template"",
            ""description"": ""Addon soundevents manifest file"",
            ""source"": ""{AlyxLib}/templates/resource_manifest.txt"",
            ""destination"": ""{AddonContent}/resourcemanifests/{AddonFolderName}_addon_resources.vrman"",
            ""replacements"": ""{AddonFolderName}""
          },
          {
            ""type"": ""delete"",
            ""description"": ""Delete default soundevents manifest file"",
            ""source"": ""{AddonContent}/resourcemanifests/addon_template_addon_resources.vrman"",
            ""rules"": [
              {
                ""type"": ""Hash"",
                ""value"": ""495d7301afadbed3eece2d16250608b4e4c9529fd3d34a8f93fbf61479c6ab13"",
                ""description"": ""Default soundevents manifest file has been modified, so it won't be deleted""
              }
            ]
          }
        ]

      }
    }
    ";

    private string _root = null!;

    [TestInitialize]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "FileDeploymentTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private DeploymentManifest CreateTempManifest()
    {
        var manifest = DeploymentManifest.LoadFromString(SampleJson);

        manifest.AddVariable("AlyxLib", () => Path.Combine(_root, "alyxlib"));
        manifest.AddVariable("AddonContent", () => Path.Combine(_root, "content"));
        manifest.AddVariable("AddonGame", () => Path.Combine(_root, "game"));
        manifest.AddVariable("ModName", () => "MY_MOD");
        manifest.AddVariable("AddonFolderName", () => "my_addon");

        return manifest;
    }

    private void CreateAlyxLibFile(string relativePath, string content = "")
    {
        var path = Path.Combine(_root, "alyxlib", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static void AssertSampleCategories(DeploymentManifest manifest)
    {
        CollectionAssert.AreEquivalent(new[] { "vscript", "editor-vscode", "panorama", "sounds" }, manifest.Categories.Keys.ToArray());
        Assert.IsTrue(manifest.HasCategory("VSCRIPT"), "Category names should be case insensitive");

        var template = manifest.Categories["sounds"].OfType<TemplateFileOperation>().Single();
        Assert.AreEqual(1, template.Replacements.Count);

        var symlinks = manifest.Categories["editor-vscode"].OfType<SymlinkFileOperation>().Count();
        Assert.AreEqual(2, symlinks);
    }

    [TestMethod()]
    public void LoadFromStringTest()
    {
        AssertSampleCategories(DeploymentManifest.LoadFromString(SampleJson));
    }

    [TestMethod()]
    public void LoadFromFileTest()
    {
        var path = Path.Combine(_root, "manifest.json");
        File.WriteAllText(path, SampleJson);

        AssertSampleCategories(DeploymentManifest.LoadFromFile(path));
    }

    [TestMethod()]
    public void DeployCategoryCopiesFilesTest()
    {
        CreateAlyxLibFile("panorama/scripts/custom_game/panorama_lua.js", "lua");
        CreateAlyxLibFile("panorama/scripts/custom_game/panoramadoc.js", "doc");

        var result = CreateTempManifest().DeployCategory("panorama");

        Assert.AreEqual(2, result.SuccessfulOperations);
        Assert.AreEqual(0, result.FailedOperations);

        var destination = Path.Combine(_root, "content", "panorama", "scripts", "custom_game");
        Assert.AreEqual("lua", File.ReadAllText(Path.Combine(destination, "panorama_lua.js")));
        Assert.AreEqual("doc", File.ReadAllText(Path.Combine(destination, "panoramadoc.js")));
    }

    private DeploymentManifest DeployPanorama()
    {
        CreateAlyxLibFile("panorama/scripts/custom_game/panorama_lua.js", "lua");
        CreateAlyxLibFile("panorama/scripts/custom_game/panoramadoc.js", "doc");

        var manifest = CreateTempManifest();
        manifest.DeployCategory("panorama");
        return manifest;
    }

    [TestMethod()]
    public void UndeployRemovesEmptyFoldersUpToProtectedFolderTest()
    {
        var manifest = DeployPanorama();
        var content = Path.Combine(_root, "content");
        manifest.ProtectedFolders = [content];

        var result = manifest.UndeployCategory("panorama", ignoreRemoveFlags: true);

        Assert.AreEqual(2, result.SuccessfulOperations);
        Assert.IsFalse(Directory.Exists(Path.Combine(content, "panorama")));
        Assert.IsTrue(Directory.Exists(content), "The protected folder should never be removed");
    }

    [TestMethod()]
    public void UndeployKeepsFoldersWithHiddenFilesTest()
    {
        var manifest = DeployPanorama();
        var content = Path.Combine(_root, "content");
        manifest.ProtectedFolders = [content];

        var hiddenFile = Path.Combine(content, "panorama", "user_file.txt");
        File.WriteAllText(hiddenFile, "");
        File.SetAttributes(hiddenFile, FileAttributes.Hidden);

        manifest.UndeployCategory("panorama", ignoreRemoveFlags: true);

        Assert.IsFalse(Directory.Exists(Path.Combine(content, "panorama", "scripts")));
        Assert.IsTrue(File.Exists(hiddenFile));
    }

    [TestMethod()]
    public void UndeployWithoutProtectedFolderOnlyRemovesImmediateParentTest()
    {
        var manifest = DeployPanorama();

        manifest.UndeployCategory("panorama", ignoreRemoveFlags: true);

        var scripts = Path.Combine(_root, "content", "panorama", "scripts");
        Assert.IsFalse(Directory.Exists(Path.Combine(scripts, "custom_game")));
        Assert.IsTrue(Directory.Exists(scripts));
    }

    [TestMethod()]
    public void DeleteRemovesEmptyFoldersOnlyInsideProtectedFolderTest()
    {
        var manifest = CreateTempManifest();
        var content = Path.Combine(_root, "content");
        var soundevents = Path.Combine(content, "soundevents");
        Directory.CreateDirectory(soundevents);
        File.WriteAllText(Path.Combine(soundevents, "addon_template_soundevents.vsndevts"), "modified");

        // Without a protected folder the emptied folder is left alone
        manifest.Categories["sounds"].OfType<DeleteFileOperation>().First().ExecuteWithoutRules();
        Assert.IsTrue(Directory.Exists(soundevents));

        File.WriteAllText(Path.Combine(soundevents, "addon_template_soundevents.vsndevts"), "modified");
        manifest.ProtectedFolders = [content];
        manifest.Categories["sounds"].OfType<DeleteFileOperation>().First().ExecuteWithoutRules();

        Assert.IsFalse(Directory.Exists(soundevents));
        Assert.IsTrue(Directory.Exists(content));
    }

    [TestMethod()]
    public void DeployCategoryWithMissingSourceFailsTest()
    {
        var result = CreateTempManifest().DeployCategory("panorama");

        Assert.AreEqual(0, result.SuccessfulOperations);
        Assert.AreEqual(2, result.FailedOperations);
        Assert.IsFalse(File.Exists(Path.Combine(_root, "content", "panorama", "scripts", "custom_game", "panorama_lua.js")));
    }

    [TestMethod()]
    public void DeployTemplateReplacesValuesTest()
    {
        CreateAlyxLibFile("templates/soundevents.txt", "sounds");
        CreateAlyxLibFile("templates/resource_manifest.txt", "manifest for {0}");

        CreateTempManifest().DeployCategory("sounds");

        var manifestFile = Path.Combine(_root, "content", "resourcemanifests", "my_addon_addon_resources.vrman");
        Assert.AreEqual("manifest for my_addon", File.ReadAllText(manifestFile));
        Assert.IsTrue(File.Exists(Path.Combine(_root, "content", "soundevents", "my_addon_soundevents.vsndevts")));
    }
}