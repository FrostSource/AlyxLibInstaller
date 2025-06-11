using FileDeployment;
using FileDeployment.Operations;
using FileDeploymentTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Tests;

[TestClass()]
public class DeploymentManifestTests
{
    //    private const string SampleJson = @"
    //{
    //    ""DefaultRules"": [
    //        {
    //            ""Type"": ""Hash"",
    //            ""Value"": ""12345""
    //        },
    //        {
    //            ""Type"": ""FileExists""
    //        }
    //    ],

    //    ""categories"": {
    //        ""panorama"": [
    //            {
    //                ""type"": ""copy"",
    //                ""source"": ""{AlyxLib}/panorama/scripts/custom_game/panorama_lua.js"",
    //                ""destination"": ""{AddonContent}/panorama/scripts/custom_game/panorama_lua.js"",
    //                ""rules"": [
    //                    {
    //                        ""Type"": ""Hash"",
    //                        ""value"": ""12345""
    //                    },
    //                    {
    //                        ""Type"": ""FileExists""
    //                    }
    //                ]
    //            },
    //            {
    //                ""type"": ""delete"",
    //                ""source"": ""{AlyxLib}/panorama/scripts/custom_game/panoramadoc.js"",
    //                ""rules"": ""FileNotExists""
    //            },
    //            {
    //                ""type"": ""template"",
    //                ""source"": ""template_file.txt"",
    //                ""destination"": ""destination.lua"",
    //                ""replacements"": ""{AddonFolderName}""
    //            }
    //        ]
    //    }
    //}";

    private const string SampleJson = @"
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

// These 2 rules were removed because ReplaceExistingFiles is false for AlyxLib so they won't be overwritten
//""rules"": [
//    {
//    ""type"": ""FileDoesNotExist"",
//    ""target"": ""destination"",
//    ""description"": ""Addon soundevents file already exists, so it will not be replaced""
//    }
//]

//""rules"": [
//    {
//    ""type"": ""FileDoesNotExist"",
//    ""target"": ""destination"",
//    ""description"": ""Addon resource manifest file already exists, so it will not be replaced"",
//    }
//]

    //private const string SampleJson = @"
    //{

    //  ""Categories"": {

    //    ""sounds"": [
    //      {
    //        ""type"": ""copy"",
    //        ""source"": ""{AlyxLib}/templates/soundevents.txt"",
    //        ""destination"": ""{AddonContent}/soundevents/{AddonFolderName}_soundevents.vsndevts""
    //      }
    //    ]

    //  }
    //}
    //";

    private static DeploymentManifest CreateTestManifest()
    {
        var manifest = DeploymentManifest.LoadFromString(SampleJson);

        manifest.AddVariable("AlyxLib", () => "C://AlyxLib");
        manifest.AddVariable("AddonContent", () => "C://content/my_addon");
        manifest.AddVariable("AddonGame", () => "C://game/my_addon");
        manifest.AddVariable("ModName", () => "MY_MOD");
        manifest.AddVariable("AddonFolderName", () => "my_addon");

        return manifest;
    }

    private static void PrintManifest(DeploymentManifest manifest)
    {
        Console.WriteLine($"Default Rules ({manifest.DefaultRules?.Count.ToString() ?? "null"}):");
        if (manifest.DefaultRules != null)
        {
            foreach (var rule in manifest.DefaultRules)
            {
                Console.WriteLine($"  Class: {rule.GetType().Name}");
                Console.WriteLine($"  Value: {(rule is IValidationRuleWithValue v ? v.Value ?? "N/A" : "N/A")}");
                //Console.WriteLine($"  Type: {rule.Type}");
                //Console.WriteLine($"  Value: {rule.Value ?? "N/A"}");
            }
        }
        else
        {
            Console.WriteLine("  N/A");
        }

        Console.WriteLine();

        Console.WriteLine($"Categories ({manifest.Categories.Count}):\n");
        foreach (var category in manifest.Categories)
        {
            Console.WriteLine($"  {category.Key} ({category.Value.Count}):");
            foreach (var operation in category.Value)
            {
                //var OP = operation;
                //var tst = operation.Destination;
                //var jjj = $"{operation.Destination ?? "lol"}";
                //Console.WriteLine($"    BLEBLE: {operation.Destination ?? "N/A"}");
                Console.WriteLine($"    Class: {operation.GetType().Name}");
                //Console.WriteLine($"    Type: {operation.Type}");
                Console.WriteLine($"    Source: {operation.Source ?? "N/A"}");
                Console.WriteLine($"    Destination: {(operation is IFileOperationWithDestination d ? d.Destination ?? "N/A" : "N/A")}");
                if (operation is TemplateFileOperation templateOperation)
                {
                    Console.WriteLine($"    Replacements ({templateOperation.Replacements?.Count ?? 0}):");
                    if (templateOperation.Replacements != null)
                    {
                        foreach (var replacement in templateOperation.Replacements)
                        {
                            Console.WriteLine($"      {replacement}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("      N/A");
                    }
                }
                Console.WriteLine($"    Rules ({operation.Rules?.Count.ToString() ?? "null"}):");
                if (operation.Rules != null)
                {
                    foreach (var rule in operation.Rules)
                    {
                        Console.WriteLine($"      Class: {rule.GetType().Name}");
                        Console.WriteLine($"      Value: {(rule is IValidationRuleWithValue v ? v.Value ?? "N/A" : "N/A")}");
                        //Console.WriteLine($"      Type: {rule.Type}");
                        // Value only exists for IValidationRuleWithValue
                        //Console.WriteLine($"      Value: {rule.Value ?? "N/A"}");
                        Console.WriteLine($"      Target: {rule.Target.ToString() ?? "N/A"}");
                    }
                }
                else
                {
                    Console.WriteLine("      N/A");
                }
                Console.WriteLine();
            }
        }
    }

    [TestMethod()]
    public void ApplyDefaultChecksTest()
    {
        
    }

    [TestMethod()]
    public void LoadFromStringTest()
    {
        
    }

    [SupportedOSPlatform("windows")]
    [TestMethod()]
    public void FakeDeploymentTest()
    {


        var manifest = DeploymentManifest.LoadFromString(SampleJson);
        //manifest.Logger = new Logging.ConsoleLogger();
        //manifest.AddVariable("AlyxLib", () => "C://AlyxLib");
        //manifest.AddVariable("AddonContent", () => "C://content/my_addon");
        //manifest.AddVariable("AddonGame", () => "C://game/my_addon");

        manifest.ReplaceExistingSymlinks = true;
        manifest.Logger = new AlyxLibFileDeploymentLogger();
        manifest.AddVariable("AlyxLib", () => @"C:\AlyxLibInstallerTest\alyxlib");
        manifest.AddVariable("AddonContent", () => @"C:\AlyxLibInstallerTest\content");
        manifest.AddVariable("AddonGame", () => @"C:\AlyxLibInstallerTest\game");
        manifest.AddVariable("ModName", () => "MY_MOD");
        manifest.AddVariable("AddonFolderName", () => "my_addon");

        manifest.DeployAllCategories();
    }

    [TestMethod()]
    public void LoadFromFileTest()
    {
        Assert.Fail();
    }
}