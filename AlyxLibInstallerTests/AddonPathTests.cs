using AlyxLibInstallerShared.ViewModels;

namespace AlyxLibInstaller.Tests;

[TestClass]
public class AddonPathTests
{
    private const string Addons = @"C:\Steam\steamapps\common\Half-Life Alyx\content\hlvr_addons";

    [TestMethod]
    [DataRow(Addons + @"\my_addon", "my_addon")]
    [DataRow(Addons + @"\my_addon\", "my_addon")]
    [DataRow(Addons + @"\my_addon\scripts\vscripts", "my_addon")]
    [DataRow(@"C:\Steam\steamapps\common\Half-Life Alyx\game\hlvr_addons\my_addon\maps", "my_addon")]
    [DataRow(@"D:\somewhere\else\my_addon", "my_addon")]
    public void GetAddonNameFromPath_FindsAddonFolder(string path, string expected)
    {
        Assert.AreEqual(expected, AlyxLibInstallerSharedViewModel.GetAddonNameFromPath(path));
    }

    [TestMethod]
    public void GetAddonNameFromPath_AddonsFolderItselfHasNoAddon()
    {
        Assert.IsNull(AlyxLibInstallerSharedViewModel.GetAddonNameFromPath(Addons));
    }
}
