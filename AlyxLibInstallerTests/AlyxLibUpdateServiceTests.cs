using AlyxLib;
using AlyxLibInstallerShared;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;
using Semver;

namespace AlyxLibInstallerTests;

[TestClass]
public class AlyxLibUpdateServiceTests
{
    private string _root = null!;
    private string _alyxLibPath = null!;
    private FakeDialogService _dialogs = null!;
    private AlyxLibManager _manager = null!;
    private Settings _settings = null!;

    private List<string> _downloads = null!;
    private List<string> _setPaths = null!;
    private List<bool> _loadingStates = null!;
    private List<Exception> _loggedExceptions = null!;
    private int _compareCount;

    private Exception? _downloadException;
    private VersionComparisonResult _comparison = VersionComparisonResult.Empty;

    [TestInitialize]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "AlyxLibUpdateServiceTests_" + Guid.NewGuid().ToString("N"));
        _alyxLibPath = Path.Combine(_root, "alyxlib");
        Directory.CreateDirectory(_alyxLibPath);

        _dialogs = new FakeDialogService();
        _manager = new AlyxLibManager { Logger = null };
        _settings = new Settings();

        _downloads = [];
        _setPaths = [];
        _loadingStates = [];
        _loggedExceptions = [];
        _compareCount = 0;
        _downloadException = null;
        _comparison = VersionComparisonResult.Empty;
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private AlyxLibUpdateService CreateService() => new(
        _dialogs,
        _manager,
        _settings,
        path => { _setPaths.Add(path); return true; },
        (loading, _) => _loadingStates.Add(loading),
        logException: _loggedExceptions.Add)
    {
        DownloadRepository = path =>
        {
            _downloads.Add(path);
            return _downloadException == null ? Task.CompletedTask : Task.FromException(_downloadException);
        },
        CompareVersions = () =>
        {
            _compareCount++;
            return Task.FromResult(_comparison);
        }
    };

    private static VersionComparisonResult Comparison(string local, string remote)
    {
        var localVersion = SemVersion.Parse(local);
        var remoteVersion = SemVersion.Parse(remote);
        return new VersionComparisonResult(SemVersion.ComparePrecedence(remoteVersion, localVersion), localVersion, remoteVersion);
    }

    private void UseExistingAlyxLib(bool isGitRepository = false)
    {
        if (isGitRepository)
            Directory.CreateDirectory(Path.Combine(_alyxLibPath, ".git"));

        _manager.SetAlyxLibPath(_alyxLibPath);
    }

    [TestMethod]
    public async Task UpdateDownloadsOverCurrentFolder()
    {
        UseExistingAlyxLib();

        await CreateService().Update();

        Assert.AreEqual(0, _dialogs.PickFolderCount);
        CollectionAssert.AreEqual(new[] { _alyxLibPath }, _downloads);
        CollectionAssert.AreEqual(new[] { _alyxLibPath }, _setPaths);
    }

    [TestMethod]
    public async Task UpdateGitCloneCancelledDoesNotDownload()
    {
        UseExistingAlyxLib(isGitRepository: true);
        _dialogs.Respond(DialogResult.None);

        await CreateService().Update();

        Assert.AreEqual("AlyxLib is a Git repository", _dialogs.Shown.Single().Config?.Title);
        Assert.AreEqual(0, _dialogs.PickFolderCount);
        Assert.AreEqual(0, _downloads.Count);
    }

    [TestMethod]
    public async Task UpdateGitCloneDownloadsToNewFolder()
    {
        UseExistingAlyxLib(isGitRepository: true);
        _dialogs.Respond(DialogResult.Primary);
        _dialogs.PickFolderResult = Path.Combine(_root, "other");

        await CreateService().Update();

        CollectionAssert.AreEqual(new[] { Path.Combine(_root, "other", "alyxlib") }, _downloads);
    }

    [TestMethod]
    public async Task DownloadToFolderNamedAlyxLibIsNotNested()
    {
        _dialogs.PickFolderResult = _alyxLibPath;

        await CreateService().DownloadAlyxLib();

        CollectionAssert.AreEqual(new[] { _alyxLibPath }, _downloads);
    }

    [TestMethod]
    public async Task DownloadWithoutPickingFolderIsCancelled()
    {
        _dialogs.PickFolderResult = null;

        await CreateService().DownloadAlyxLib(newLocation: true);

        Assert.AreEqual(1, _dialogs.PickFolderCount);
        Assert.AreEqual(0, _downloads.Count);
        Assert.AreEqual(0, _loadingStates.Count);
    }

    [TestMethod]
    public async Task DownloadConnectionFailureShowsWarning()
    {
        UseExistingAlyxLib();
        _downloadException = new HttpRequestException("No connection");

        await CreateService().DownloadAlyxLib();

        Assert.AreEqual("Connection Failed", _dialogs.Shown.Single().Config?.Title);
        Assert.AreEqual(0, _setPaths.Count);
        Assert.IsFalse(_loadingStates.Last(), "Loading overlay should be hidden after a failure");
    }

    [TestMethod]
    public async Task DownloadExtractFailureShowsWarning()
    {
        UseExistingAlyxLib();
        _downloadException = new IOException("File in use");

        await CreateService().DownloadAlyxLib();

        var config = _dialogs.Shown.Single().Config;
        Assert.AreEqual("Download Failed", config?.Title);
        StringAssert.Contains(config?.Message, "File in use");
        Assert.AreEqual(0, _setPaths.Count);
        Assert.IsFalse(_loadingStates.Last());
    }

    [TestMethod]
    public async Task CheckForUpdatesConnectionFailed()
    {
        _comparison = VersionComparisonResult.Failed;

        await CreateService().CheckForUpdates();

        Assert.AreEqual("Connection Failed", _dialogs.Shown.Single().Config?.Title);
        Assert.IsFalse(_loadingStates.Last());
    }

    [TestMethod]
    public async Task CheckForUpdatesUpToDate()
    {
        _comparison = Comparison("1.2.0", "1.2.0");

        await CreateService().CheckForUpdates();

        Assert.AreEqual("Up to date", _dialogs.Shown.Single().Config?.Title);
    }

    [TestMethod]
    public async Task CheckForUpdatesAhead()
    {
        _comparison = Comparison("1.3.0", "1.2.0");

        await CreateService().CheckForUpdates();

        Assert.AreEqual("Ahead of latest version", _dialogs.Shown.Single().Config?.Title);
    }

    [TestMethod]
    public async Task CheckForUpdatesNewerVersionDownloads()
    {
        UseExistingAlyxLib();
        _comparison = Comparison("1.2.0", "1.3.0");
        _dialogs.Respond(DialogResult.Primary);

        await CreateService().CheckForUpdates();

        Assert.AreEqual("Update Available", _dialogs.Shown.Single().Config?.Title);
        CollectionAssert.AreEqual(new[] { _alyxLibPath }, _downloads);
    }

    [TestMethod]
    public async Task CheckForUpdatesComparisonErrorIsLogged()
    {
        var service = new AlyxLibUpdateService(_dialogs, _manager, _settings, _ => true, (_, _) => { }, logException: _loggedExceptions.Add)
        {
            CompareVersions = () => throw new InvalidOperationException("Bad version file")
        };

        await service.CheckForUpdates();

        Assert.IsInstanceOfType<InvalidOperationException>(_loggedExceptions.Single());
        Assert.AreEqual("Connection Failed", _dialogs.Shown.Single().Config?.Title);
    }

    [TestMethod]
    public async Task StartupCheckSkippedWithoutAlyxLib()
    {
        await CreateService().CheckForUpdatesOnStartup();

        Assert.AreEqual(0, _compareCount);
    }

    [TestMethod]
    public async Task StartupCheckSkippedWhenTurnedOff()
    {
        UseExistingAlyxLib();
        _settings.SetDontShowAgain("StartupUpdate", true);

        await CreateService().CheckForUpdatesOnStartup();

        Assert.AreEqual(0, _compareCount);
    }

    [TestMethod]
    public async Task StartupCheckIsQuietWhenUpToDate()
    {
        UseExistingAlyxLib();
        _comparison = Comparison("1.2.0", "1.2.0");

        await CreateService().CheckForUpdatesOnStartup();

        Assert.AreEqual(1, _compareCount);
        Assert.AreEqual(0, _dialogs.Shown.Count);
    }

    [TestMethod]
    public async Task StartupCheckRemembersDontShowAgain()
    {
        UseExistingAlyxLib();
        _comparison = Comparison("1.2.0", "1.3.0");
        _dialogs.Respond(DialogResult.None, checkboxChecked: true);

        await CreateService().CheckForUpdatesOnStartup();

        Assert.IsTrue(_settings.GetDontShowAgain("StartupUpdate"));
        Assert.AreEqual(0, _downloads.Count);
    }

    [TestMethod]
    public async Task StartupCheckDownloadsWhenAccepted()
    {
        UseExistingAlyxLib();
        _comparison = Comparison("1.2.0", "1.3.0");
        _dialogs.Respond(DialogResult.Primary, checkboxChecked: false);

        await CreateService().CheckForUpdatesOnStartup();

        Assert.IsFalse(_settings.GetDontShowAgain("StartupUpdate"));
        CollectionAssert.AreEqual(new[] { _alyxLibPath }, _downloads);
    }
}
