using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;

namespace AlyxLibInstallerTests;

[TestClass]
public class WorkshopUploadServiceTests
{
    private const ulong ItemId = 3835625705;

    private TestFileSystem _fs = null!;
    private FakeDialogService _dialogs = null!;
    private FakeWorkshopClient _client = null!;
    private WorkshopItemStore _store = null!;
    private string? _packedControlFile;
    private int _connectCount;
    private bool _isLoading;

    [TestInitialize]
    public void Setup()
    {
        _fs = new TestFileSystem();
        _dialogs = new FakeDialogService();
        _client = new FakeWorkshopClient
        {
            Item = new WorkshopItemDetails(ItemId, "Resin Watch", "Tracks resin", WorkshopVisibility.Public, ["Alyx Addon"], [WorkshopProperties.AlyxLibItemId], "", DateTimeOffset.Now)
        };
        _store = new WorkshopItemStore(_fs.PathOf("vpks"));
        _fs.WriteFile("game/models/watch.vmdl_c");
        _fs.WriteFile("game/save/quick.sav");
        _fs.WriteFile("game/panorama/scripts/custom_game/panorama_lua.vjs_c");
        Directory.CreateDirectory(_fs.PathOf("content"));
    }

    [TestCleanup]
    public void Cleanup() => _fs.Dispose();

    private WorkshopUploadService CreateService(Func<IWorkshopClient>? connect = null)
    {
        var packer = new VpkPacker("vpk.exe", args =>
        {
            // Arguments are: k "<vpk>" "<control file>"
            var paths = args.Split('"');
            _packedControlFile = File.ReadAllText(paths[3]);
            File.WriteAllText(paths[1], "pack");
            return (0, "");
        });

        return new WorkshopUploadService(
            _dialogs,
            connect ?? (() => { _connectCount++; return _client; }),
            _store,
            packer,
            (loading, _) => _isLoading = loading,
            logException: _ => { });
    }

    private Task<WorkshopUploadResult> Upload(Func<IWorkshopClient>? connect = null) =>
        CreateService(connect).UploadAsync(new WorkshopUploadRequest(
            "resin_watch",
            _fs.PathOf("content"),
            _fs.PathOf("game"),
            ["TMP:save/**"],
            [_fs.PathOf(@"game\panorama\scripts\custom_game\panorama_lua.vjs_c")]));

    private void Link(Action<WorkshopProperties>? configure = null)
    {
        var properties = new WorkshopProperties { ItemId = ItemId };
        configure?.Invoke(properties);
        properties.Save(_fs.PathOf("content"));
    }

    [TestMethod]
    public void UploadsContentWithChangeNotesAndLeavesAddonUnchanged()
    {
        Link();
        _dialogs.Respond(DialogResult.Primary, inputText: "  Fixed the watch  ");

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);

        var update = _client.Submitted.Single();
        Assert.AreEqual(_store.GetItemFolder(ItemId), update.ContentFolder);
        Assert.AreEqual("Fixed the watch", update.ChangeNote);
        Assert.IsNull(update.Title);
        Assert.IsNull(update.Description);
        Assert.IsNull(update.RequiredItems);
        Assert.IsTrue(File.Exists(_store.GetVpkPath(ItemId)));
        Assert.IsTrue(_client.Disposed);
        Assert.IsFalse(_isLoading);

        // Removal rules and AlyxLib files are left out of the pack, without touching the game folder
        StringAssert.Contains(_packedControlFile, "models/watch.vmdl_c");
        Assert.IsFalse(_packedControlFile!.Contains("quick.sav"));
        Assert.IsFalse(_packedControlFile.Contains("panorama_lua"));
        Assert.IsTrue(File.Exists(_fs.PathOf("game/save/quick.sav")));

        var published = _store.GetItemsForAddon("resin_watch").Single();
        Assert.AreEqual("Resin Watch", published.Title);
    }

    [TestMethod]
    public void EmptyChangeNotesAreNotSent()
    {
        Link();
        _dialogs.Respond(DialogResult.Primary, inputText: "  ");

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);
        Assert.IsNull(_client.Submitted.Single().ChangeNote);
    }

    [TestMethod]
    public void CancellingChangeNotesDoesNotPackOrUpload()
    {
        Link();
        _dialogs.Respond(DialogResult.None);

        Assert.AreEqual(WorkshopUploadResult.Cancelled, Upload().Result);
        Assert.AreEqual(0, _client.Submitted.Count);
        Assert.IsNull(_packedControlFile);
    }

    [TestMethod]
    public void UnlinkedAddonWithoutPublishedItemsDoesNotConnect()
    {
        Assert.AreEqual(WorkshopUploadResult.Cancelled, Upload().Result);

        Assert.AreEqual(0, _connectCount);
        Assert.AreEqual("No Workshop Item", _dialogs.Shown.Single().Config!.Title);
    }

    [TestMethod]
    public void UnlinkedAddonCanBeLinkedToItsNewestPublishedItem()
    {
        _store.Save(new PublishData(111, "Old test", "resin_watch", DateTimeOffset.FromUnixTimeSeconds(1000)));
        _store.Save(new PublishData(ItemId, "Resin Watch", "resin_watch", DateTimeOffset.FromUnixTimeSeconds(2000)));
        _dialogs.Respond(DialogResult.Primary);
        _dialogs.Respond(DialogResult.Primary, inputText: "Notes");

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);

        StringAssert.Contains(_dialogs.Shown[0].Config!.Message, "Old test (111)");
        Assert.AreEqual(ItemId, WorkshopProperties.Load(_fs.PathOf("content"))!.ItemId);
        Assert.AreEqual(ItemId, _client.Submitted.Single().ItemId);
    }

    [TestMethod]
    public void DecliningLinkDoesNotSaveIt()
    {
        _store.Save(new PublishData(ItemId, "Resin Watch", "resin_watch", DateTimeOffset.Now));
        _dialogs.Respond(DialogResult.None);

        Assert.AreEqual(WorkshopUploadResult.Cancelled, Upload().Result);
        Assert.IsNull(WorkshopProperties.Load(_fs.PathOf("content")));
    }

    [TestMethod]
    public void MismatchUploadAnywayKeepsWorkshopProperties()
    {
        Link(properties => properties.Title = "Local title");
        _dialogs.Respond(DialogResult.Primary);
        _dialogs.Respond(DialogResult.Primary, inputText: "Notes");

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);

        StringAssert.Contains(_dialogs.Shown[0].Config!.Message, "• Title");
        Assert.IsNull(_client.Submitted.Single().Title);
    }

    [TestMethod]
    public void MismatchUpdateWorkshopSendsOnlyChangedProperties()
    {
        Link(properties =>
        {
            properties.Title = "Local title";
            properties.Description = "Tracks resin";
            properties.RequiredItems = [];
        });
        _dialogs.Respond(DialogResult.Secondary);
        _dialogs.Respond(DialogResult.Primary, inputText: "Notes");

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);

        var update = _client.Submitted.Single();
        Assert.AreEqual("Local title", update.Title);
        Assert.IsNull(update.Description);
        CollectionAssert.AreEqual(Array.Empty<ulong>(), update.RequiredItems!.ToArray());
        Assert.AreEqual("Local title", _store.GetItemsForAddon("resin_watch").Single().Title);
    }

    [TestMethod]
    public void MismatchCancelDoesNotUpload()
    {
        Link(properties => properties.Visibility = WorkshopVisibility.Private);
        _dialogs.Respond(DialogResult.None);

        Assert.AreEqual(WorkshopUploadResult.Cancelled, Upload().Result);
        Assert.AreEqual(0, _client.Submitted.Count);
    }

    [TestMethod]
    public void MismatchOpenPropertiesStopsBeforePacking()
    {
        Link(properties => properties.Title = "Local title");
        _dialogs.Respond(DialogResult.Tertiary);

        Assert.AreEqual(WorkshopUploadResult.OpenProperties, Upload().Result);
        Assert.AreEqual(0, _client.Submitted.Count);
        Assert.IsNull(_packedControlFile);
        Assert.AreEqual("Open Properties", _dialogs.Shown.Single().Config!.TertiaryButtonText);
    }

    [TestMethod]
    public void LinkedDescriptionFileIsSentWhenUpdatingWorkshop()
    {
        _fs.WriteFile("content/workshop_description.txt", "From the file");
        Link(properties => properties.DescriptionFile = "workshop_description.txt");
        _dialogs.Respond(DialogResult.Secondary);
        _dialogs.Respond(DialogResult.Primary);

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);
        Assert.AreEqual("From the file", _client.Submitted.Single().Description);
    }

    [TestMethod]
    public void MissingDescriptionFileStopsBeforeConnecting()
    {
        Link(properties => properties.DescriptionFile = "missing.txt");

        Assert.AreEqual(WorkshopUploadResult.Failed, Upload().Result);
        Assert.AreEqual(0, _connectCount);
        Assert.AreEqual("Description File Missing", _dialogs.Shown.Single().Config!.Title);
    }

    [TestMethod]
    public void DeletedItemIsReported()
    {
        Link();
        _client.Item = null;

        Assert.AreEqual(WorkshopUploadResult.Failed, Upload().Result);
        Assert.AreEqual("Workshop Item Not Found", _dialogs.Shown.Single().Config!.Title);
        Assert.IsTrue(_client.Disposed);
    }

    [TestMethod]
    public void SteamErrorsAreShownAndHideLoading()
    {
        Link();

        Assert.AreEqual(WorkshopUploadResult.Failed, Upload(() => throw new WorkshopException("Steam isn't running")).Result);

        Assert.AreEqual("Steam isn't running", _dialogs.Shown.Single().Config!.Message);
        Assert.IsFalse(_isLoading);
    }

    [TestMethod]
    public void LegalAgreementIsShownAfterUpload()
    {
        Link();
        _client.NeedsLegalAgreement = true;
        _dialogs.Respond(DialogResult.Primary);

        Assert.AreEqual(WorkshopUploadResult.Uploaded, Upload().Result);
        StringAssert.Contains(_dialogs.Shown.Last().Config!.Message, WorkshopUploadService.LegalAgreementUrl);
    }
}

/// <summary>
/// Workshop client that returns a set item and records updates instead of talking to Steam.
/// </summary>
internal class FakeWorkshopClient : IWorkshopClient
{
    public WorkshopItemDetails? Item { get; set; }
    public bool NeedsLegalAgreement { get; set; }

    public List<WorkshopItemUpdate> Submitted { get; } = [];
    public bool Disposed { get; private set; }

    public Task<WorkshopItemDetails?> GetItemAsync(ulong itemId, CancellationToken cancellationToken = default) => Task.FromResult(Item);

    public Task<WorkshopSubmitResult> SubmitUpdateAsync(WorkshopItemUpdate update, IProgress<WorkshopUploadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Submitted.Add(update);
        return Task.FromResult(new WorkshopSubmitResult(NeedsLegalAgreement));
    }

    public void Dispose() => Disposed = true;
}
