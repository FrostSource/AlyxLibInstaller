using AlyxLib.Workshop;

namespace AlyxLibInstallerTests;

[TestClass]
public class VpkPackerTests
{
    private const string RealVpkExe = @"C:\Program Files (x86)\Steam\steamapps\common\Half-Life Alyx\game\bin\win64\vpk.exe";

    private TestFileSystem _fs = null!;

    [TestInitialize]
    public void Setup() => _fs = new TestFileSystem();

    [TestCleanup]
    public void Cleanup() => _fs.Dispose();

    private string GamePath => _fs.PathOf("game");

    private string[] PackPaths(IEnumerable<string>? removalGlobs = null, string? alyxLibPath = null) =>
        [.. VpkPacker.GetFiles(GamePath, removalGlobs ?? [], alyxLibPath).Select(file => file.PackPath)];

    [TestMethod]
    public void ToolsFilesAreLeftOut()
    {
        _fs.WriteFile("game/models/watch.vmdl_c");
        _fs.WriteFile("game/tools_asset_info.bin");
        _fs.WriteFile("game/tools_thumbnail_cache.bin");
        _fs.WriteFile("game/_bakeresourcecache/maps/cubemap.vtex_c");

        CollectionAssert.AreEqual(new[] { "models/watch.vmdl_c" }, PackPaths());
    }

    [TestMethod]
    public void RemovalRulesAreLeftOutIncludingTemporaryMoves()
    {
        _fs.WriteFile("game/models/watch.vmdl_c");
        _fs.WriteFile("game/save/quick.sav");
        _fs.WriteFile("game/maps/test.vmap_c");
        _fs.WriteFile("game/maps/keep.vmap_c");

        var paths = PackPaths(["TMP:save/**", "maps/*.vmap_c", "!maps/keep.vmap_c"]);

        CollectionAssert.AreEqual(new[] { "maps/keep.vmap_c", "models/watch.vmdl_c" }, paths);
    }

    [TestMethod]
    public void LinkedFoldersArePackedLikeNormalFolders()
    {
        // The game scripts folder links to the content scripts folder
        _fs.WriteFile("content/scripts/vscripts/my_mod/init.lua");
        _fs.Junction("game/scripts", "content/scripts");

        var files = VpkPacker.GetFiles(GamePath, [], null);

        Assert.AreEqual("scripts/vscripts/my_mod/init.lua", files.Single().PackPath);
        Assert.AreEqual(_fs.PathOf(@"game\scripts\vscripts\my_mod\init.lua"), files.Single().SourcePath);
    }

    [TestMethod]
    public void LinksIntoAlyxLibAreLeftOut()
    {
        _fs.WriteFile("alyxlib/scripts/vscripts/alyxlib/init.lua");
        _fs.WriteFile("content/scripts/vscripts/my_mod/init.lua");
        _fs.Junction("content/scripts/vscripts/alyxlib", "alyxlib/scripts/vscripts/alyxlib");
        _fs.Junction("game/scripts", "content/scripts");

        CollectionAssert.AreEqual(new[] { "scripts/vscripts/my_mod/init.lua" }, PackPaths(alyxLibPath: _fs.PathOf("alyxlib")));
    }

    [TestMethod]
    public void LinkLoopsAreOnlyPackedOnce()
    {
        _fs.WriteFile("game/models/watch.vmdl_c");
        _fs.Junction("game/models/loop", "game");

        CollectionAssert.AreEqual(new[] { "models/watch.vmdl_c" }, PackPaths());
    }

    [TestMethod]
    public void ControlFileEscapesPaths()
    {
        var control = VpkPacker.CreateControlFile([new PackFile(@"C:\game\a.lua", "scripts/a.lua")]);

        StringAssert.Contains(control, "\"srcpath\"\t\"C:\\\\game\\\\a.lua\"");
        StringAssert.Contains(control, "\"destpath\"\t\"scripts/a.lua\"");
    }

    [TestMethod]
    public void PackReplacesExistingVpk()
    {
        var vpkPath = _fs.WriteFile(@"vpks\1\1.vpk", "old pack");
        string? arguments = null;
        var packer = new VpkPacker("vpk.exe", args =>
        {
            arguments = args;
            Assert.IsFalse(File.Exists(vpkPath), "The old pack should be removed before packing");
            File.WriteAllText(vpkPath, "new pack");
            return (0, "");
        });

        packer.Pack([new PackFile(_fs.WriteFile("game/a.lua"), "a.lua")], vpkPath);

        StringAssert.StartsWith(arguments, $"k \"{vpkPath}\"");
        Assert.AreEqual("new pack", File.ReadAllText(vpkPath));
    }

    [TestMethod]
    public void PackFailureIncludesVpkOutput()
    {
        var packer = new VpkPacker("vpk.exe", _ => (1, "Input file not found"));

        var exception = Assert.ThrowsException<VpkPackException>(() =>
            packer.Pack([new PackFile(_fs.WriteFile("game/a.lua"), "a.lua")], _fs.PathOf("vpks/1/1.vpk")));

        Assert.AreEqual("Input file not found", exception.Output);
    }

    [TestMethod]
    public void PackWithNothingToPackFails()
    {
        Assert.ThrowsException<VpkPackException>(() => new VpkPacker("vpk.exe").Pack([], _fs.PathOf("1.vpk")));
    }

    [TestMethod]
    public void PacksWithRealVpkExe()
    {
        if (!File.Exists(RealVpkExe))
            Assert.Inconclusive("Half-Life: Alyx Workshop Tools aren't installed");

        _fs.WriteFile("content/scripts/vscripts/my_mod/init.lua", "print('hello')");
        _fs.WriteFile("game/models/watch.vmdl_c", "model");
        _fs.WriteFile("game/tools_asset_info.bin");
        _fs.Junction("game/scripts", "content/scripts");
        var vpkPath = _fs.PathOf("vpks/1/1.vpk");

        new VpkPacker(RealVpkExe).Pack(VpkPacker.GetFiles(GamePath, [], null), vpkPath);

        var list = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(RealVpkExe, $"l \"{vpkPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        })!;
        var contents = list.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        list.WaitForExit();

        CollectionAssert.AreEquivalent(new[] { "models/watch.vmdl_c", "scripts/vscripts/my_mod/init.lua" }, contents);
    }
}
