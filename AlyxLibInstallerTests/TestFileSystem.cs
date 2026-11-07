namespace AlyxLibInstallerTests;

/// <summary>
/// A temporary folder for a test, with helpers for building files and links inside it.
/// </summary>
internal sealed class TestFileSystem : IDisposable
{
    private readonly List<string> _junctions = [];

    public string Root { get; } = Path.Combine(Path.GetTempPath(), "AlyxLibInstallerTests_" + Guid.NewGuid().ToString("N"));

    public TestFileSystem() => Directory.CreateDirectory(Root);

    public string PathOf(string relativePath) => Path.Combine(Root, relativePath);

    public string WriteFile(string relativePath, string contents = "test")
    {
        var path = PathOf(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    public void Junction(string relativeLinkPath, string relativeTargetPath)
    {
        var linkPath = PathOf(relativeLinkPath);
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        CreateJunction(linkPath, PathOf(relativeTargetPath));
        _junctions.Add(linkPath);
    }

    public static void CreateJunction(string linkPath, string targetPath)
    {
        // Junctions don't need admin rights or developer mode, unlike symlinks
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        })!;
        process.WaitForExit();
        Assert.AreEqual(0, process.ExitCode, "Failed to create junction for test");
    }

    public void Dispose()
    {
        // Junctions are removed first, recursive cleanup can't delete through them
        foreach (var junction in _junctions.Where(Directory.Exists))
            Directory.Delete(junction);

        if (Directory.Exists(Root))
            Directory.Delete(Root, recursive: true);
    }
}
