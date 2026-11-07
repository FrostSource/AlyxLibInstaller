using AlyxLib;

namespace AlyxLibInstaller.Tests;

[TestClass]
public class AlyxLibHelpersTests
{
    private string _rootPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "AlyxLibInstallerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_rootPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    [TestMethod]
    public void RemoveFilesNoLongerInRepository_RemovesOnlyPreviouslyExtractedFiles()
    {
        WriteFile("scripts/kept.lua");
        WriteFile("scripts/removed_upstream.lua");
        WriteFile("scripts/user_added.lua");
        File.WriteAllLines(Path.Combine(_rootPath, AlyxLibHelpers.ExtractedFilesListName),
            [Path.Combine("scripts", "kept.lua"), Path.Combine("scripts", "removed_upstream.lua")]);

        AlyxLibHelpers.RemoveFilesNoLongerInRepository(_rootPath, [Path.Combine("scripts", "kept.lua")], null);

        Assert.IsTrue(File.Exists(Path.Combine(_rootPath, "scripts", "kept.lua")));
        Assert.IsFalse(File.Exists(Path.Combine(_rootPath, "scripts", "removed_upstream.lua")));
        Assert.IsTrue(File.Exists(Path.Combine(_rootPath, "scripts", "user_added.lua")));
        CollectionAssert.AreEqual(
            new[] { Path.Combine("scripts", "kept.lua") },
            File.ReadAllLines(Path.Combine(_rootPath, AlyxLibHelpers.ExtractedFilesListName)));
    }

    [TestMethod]
    public void RemoveFilesNoLongerInRepository_WithoutPreviousListRemovesNothing()
    {
        WriteFile("scripts/old.lua");

        AlyxLibHelpers.RemoveFilesNoLongerInRepository(_rootPath, [Path.Combine("scripts", "new.lua")], null);

        Assert.IsTrue(File.Exists(Path.Combine(_rootPath, "scripts", "old.lua")));
        Assert.IsTrue(File.Exists(Path.Combine(_rootPath, AlyxLibHelpers.ExtractedFilesListName)));
    }

    [TestMethod]
    public void RemoveFilesNoLongerInRepository_IgnoresPathsOutsideFolder()
    {
        var outsidePath = Path.Combine(Path.GetDirectoryName(_rootPath)!, $"outside_{Guid.NewGuid():N}.txt");
        File.WriteAllText(outsidePath, "test");

        try
        {
            File.WriteAllLines(Path.Combine(_rootPath, AlyxLibHelpers.ExtractedFilesListName),
                [Path.Combine("..", Path.GetFileName(outsidePath))]);

            AlyxLibHelpers.RemoveFilesNoLongerInRepository(_rootPath, [], null);

            Assert.IsTrue(File.Exists(outsidePath));
        }
        finally
        {
            File.Delete(outsidePath);
        }
    }

    private void WriteFile(string relativePath, string contents = "test")
    {
        var filePath = Path.Combine(_rootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, contents);
    }
}
