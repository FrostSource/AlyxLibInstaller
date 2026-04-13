using AlyxLib.Services;

namespace AlyxLibInstaller.Tests;

[TestClass]
public class FileGlobServiceTests
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
    public void GetMatchingFileActions_UsesSharedRuleSemantics()
    {
        WriteFile("delete/me.txt");
        WriteFile("temp/me.txt");
        WriteFile("temp/skip.txt");

        var service = new FileGlobService([
            "delete/**/*.txt",
            "TMP:temp/**/*.txt",
            "!temp/skip.txt",
            "TMP:!ignored/**/*.txt"
        ]);

        var matches = service.GetMatchingFileActions(_rootPath);

        CollectionAssert.AreEqual(
            [
                new FileRemovalMatch("delete/me.txt", FileRemovalAction.Delete),
                new FileRemovalMatch("temp/me.txt", FileRemovalAction.TemporaryMove)
            ],
            matches.ToArray());
    }

    [TestMethod]
    public void ApplyRemovals_DeletesAndRestoresTemporaryFiles()
    {
        WriteFile("delete.txt", "delete");
        WriteFile("temp.txt", "temp");
        WriteFile("nested/temp2.txt", "temp2");

        var temporaryStoragePath = Path.Combine(_rootPath, ".stash");
        var service = new FileGlobService([
            "delete.txt",
            "TMP:temp.txt",
            "TMP:nested/*.txt"
        ]);

        service.ApplyRemovals(_rootPath, temporaryStoragePath);

        Assert.IsFalse(File.Exists(Path.Combine(_rootPath, "delete.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(_rootPath, "temp.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(_rootPath, "nested", "temp2.txt")));
        Assert.IsTrue(File.Exists(Path.Combine(temporaryStoragePath, "temp.txt")));
        Assert.IsTrue(File.Exists(Path.Combine(temporaryStoragePath, "nested", "temp2.txt")));

        service.RestoreTemporarilyMovedFiles(_rootPath, temporaryStoragePath);

        Assert.IsFalse(Directory.Exists(temporaryStoragePath));
        Assert.AreEqual("temp", File.ReadAllText(Path.Combine(_rootPath, "temp.txt")));
        Assert.AreEqual("temp2", File.ReadAllText(Path.Combine(_rootPath, "nested", "temp2.txt")));
    }

    private void WriteFile(string relativePath, string contents = "test")
    {
        var filePath = Path.Combine(_rootPath, relativePath);
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        File.WriteAllText(filePath, contents);
    }
}
