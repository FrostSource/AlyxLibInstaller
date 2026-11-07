using AlyxLib;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;

namespace AlyxLibInstallerTests;

[TestClass]
public class AddonInstallServiceTests
{
    private FakeDialogService _dialogs = null!;
    private FakeAddonFileOperations _files = null!;
    private List<Exception> _loggedExceptions = null!;
    private AddonInstallService _service = null!;

    private static AddonConfig AllInstalled(bool git = true, bool panorama = true) => new()
    {
        VScriptInstalled = true,
        SoundEventInstalled = true,
        PanoramaInstalled = panorama,
        GitInstalled = git
    };

    [TestInitialize]
    public void Setup()
    {
        _dialogs = new FakeDialogService();
        _files = new FakeAddonFileOperations { InstalledConfig = AllInstalled() };
        _loggedExceptions = [];
        _service = new AddonInstallService(_dialogs, _files, logException: _loggedExceptions.Add);
    }

    [TestMethod]
    public async Task UninstallSkipsWhenOptionsMatchInstalled()
    {
        var uninstalled = await _service.UninstallUntickedOptions(TestAddon.Create(), AllInstalled());

        Assert.IsFalse(uninstalled);
        Assert.AreEqual(0, _dialogs.Shown.Count);
        Assert.AreEqual(0, _files.Calls.Count);
    }

    [TestMethod]
    public async Task UninstallGitAsksBeforeDeletingRepository()
    {
        _files.GitRepository = true;
        _dialogs.Respond(DialogResult.Primary);

        var uninstalled = await _service.UninstallUntickedOptions(TestAddon.Create(), AllInstalled(git: false));

        Assert.IsTrue(uninstalled);
        Assert.AreEqual("Delete Git Repository?", _dialogs.Shown.Single().Config?.Title);
        CollectionAssert.AreEqual(new[] { nameof(IAddonFileOperations.UninstallOptions) }, _files.Calls);
    }

    [TestMethod]
    public async Task UninstallGitCancelledKeepsEverything()
    {
        _files.GitRepository = true;
        _dialogs.Respond(DialogResult.None);

        var uninstalled = await _service.UninstallUntickedOptions(TestAddon.Create(), AllInstalled(git: false, panorama: false));

        Assert.IsFalse(uninstalled);
        Assert.AreEqual(0, _files.Calls.Count);
    }

    [TestMethod]
    public async Task UninstallGitWithoutRepositoryDoesNotAsk()
    {
        _files.GitRepository = false;

        var uninstalled = await _service.UninstallUntickedOptions(TestAddon.Create(), AllInstalled(git: false));

        Assert.IsTrue(uninstalled);
        Assert.AreEqual(0, _dialogs.Shown.Count);
        CollectionAssert.AreEqual(new[] { nameof(IAddonFileOperations.UninstallOptions) }, _files.Calls);
    }

    [TestMethod]
    public async Task RemoveAllCancelledRemovesNothing()
    {
        _dialogs.Respond(DialogResult.None);

        Assert.IsFalse(await _service.RemoveAll(TestAddon.Create()));
        Assert.AreEqual(0, _files.Calls.Count);
    }

    [TestMethod]
    public async Task RemoveAllOnlyDeletesRepositoryWhenTicked()
    {
        _files.GitRepository = true;
        _dialogs.Respond(DialogResult.Primary, checkboxChecked: false);

        Assert.IsTrue(await _service.RemoveAll(TestAddon.Create()));
        Assert.AreEqual(true, _dialogs.Shown.Single().Config?.HasCheckBox);
        Assert.AreEqual(false, _files.RemovedGitRepository);
    }

    [TestMethod]
    public async Task RemoveAllDeletesRepositoryWhenTicked()
    {
        _files.GitRepository = true;
        _dialogs.Respond(DialogResult.Primary, checkboxChecked: true);

        Assert.IsTrue(await _service.RemoveAll(TestAddon.Create()));
        Assert.AreEqual(true, _files.RemovedGitRepository);
    }

    [TestMethod]
    public async Task RemoveAllWithoutRepositoryHasNoCheckbox()
    {
        _files.GitRepository = false;
        _dialogs.Respond(DialogResult.Primary, checkboxChecked: true);

        Assert.IsTrue(await _service.RemoveAll(TestAddon.Create()));
        Assert.AreEqual(false, _dialogs.Shown.Single().Config?.HasCheckBox);
        Assert.AreEqual(false, _files.RemovedGitRepository);
    }

    [TestMethod]
    public void InstallFailureIsLoggedAndReported()
    {
        var exception = new IOException("Disk full");
        _files.InstallException = exception;

        Assert.IsFalse(_service.Install(TestAddon.Create(), AllInstalled()));
        Assert.AreSame(exception, _loggedExceptions.Single());
    }

    [TestMethod]
    public void InstallSuccess()
    {
        Assert.IsTrue(_service.Install(TestAddon.Create(), AllInstalled()));
        Assert.AreEqual(0, _loggedExceptions.Count);
    }
}
