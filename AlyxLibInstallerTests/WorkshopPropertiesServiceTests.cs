using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.Services.Dialog;
using AlyxLibInstallerShared.ViewModels;

namespace AlyxLibInstallerTests;

[TestClass]
public class WorkshopPropertiesServiceTests
{
    private const ulong ItemId = 3835625705;

    private TestFileSystem _fs = null!;
    private FakeDialogService _dialogs = null!;
    private FakeWorkshopClient _client = null!;
    private WorkshopItemStore _store = null!;
    private int _connectCount;

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
        Directory.CreateDirectory(_fs.PathOf("content"));
    }

    [TestCleanup]
    public void Cleanup() => _fs.Dispose();

    private string ContentPath => _fs.PathOf("content");

    private bool Edit(Func<IWorkshopClient>? connect = null) =>
        new WorkshopPropertiesService(_dialogs, connect ?? (() => { _connectCount++; return _client; }), _store, (_, _) => { }, logException: _ => { })
            .EditAsync("resin_watch", ContentPath).Result;

    private void Link(Action<WorkshopProperties>? configure = null)
    {
        var properties = new WorkshopProperties { ItemId = ItemId };
        configure?.Invoke(properties);
        properties.Save(ContentPath);
    }

    [TestMethod]
    public void SavingTracksEveryPropertyWithoutUpdatingTheWorkshop()
    {
        Link();
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit());

        var saved = WorkshopProperties.Load(ContentPath)!;
        Assert.AreEqual("Resin Watch", saved.Title);
        Assert.AreEqual("Tracks resin", saved.Description);
        Assert.AreEqual(WorkshopVisibility.Public, saved.Visibility);
        Assert.AreEqual(0, _client.Submitted.Count);
        Assert.AreEqual(1, _connectCount);
    }

    [TestMethod]
    public void CancellingSavesNothing()
    {
        Link();
        _dialogs.Respond(DialogResult.None);

        Assert.IsFalse(Edit());
        Assert.IsNull(WorkshopProperties.Load(ContentPath)!.Title);
    }

    [TestMethod]
    public void OnlyPropertiesMarkedToUpdateAreSent()
    {
        Link(properties =>
        {
            properties.Title = "Local title";
            properties.Visibility = WorkshopVisibility.Private;
        });
        _dialogs.OnPopupShown = popup =>
        {
            var editor = (WorkshopPropertiesEditorViewModel)popup;
            editor.Mismatches.Single(mismatch => mismatch.Field == WorkshopFields.Title).UpdateWorkshop = true;
        };
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit());

        var update = _client.Submitted.Single();
        Assert.AreEqual("Local title", update.Title);
        Assert.IsNull(update.Visibility);
        Assert.IsNull(update.ContentFolder);
        Assert.IsNull(update.PreviewFile);
    }

    [TestMethod]
    public void ChangedPreviewIsUploaded()
    {
        Link();
        var preview = _fs.WriteFile("content/preview.png", "image");
        _dialogs.PickFileResult = preview;
        _dialogs.OnPopupShown = popup => ((WorkshopPropertiesEditorViewModel)popup).ChoosePreviewCommand.Execute(null);
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit());

        Assert.AreEqual(Path.GetFullPath(preview), _client.Submitted.Single().PreviewFile);
        Assert.AreEqual("preview.png", WorkshopProperties.Load(ContentPath)!.PreviewFile);
    }

    [TestMethod]
    public void InvalidPropertiesReopenTheEditor()
    {
        Link();
        var shown = 0;
        _dialogs.OnPopupShown = popup =>
        {
            var editor = (WorkshopPropertiesEditorViewModel)popup;
            editor.Title = ++shown == 1 ? "" : "Fixed title";
        };
        _dialogs.Respond(DialogResult.Primary);
        _dialogs.Respond(DialogResult.None);
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit());

        Assert.AreEqual("Check Item Properties", _dialogs.Shown[1].Config!.Title);
        Assert.AreEqual("Fixed title", WorkshopProperties.Load(ContentPath)!.Title);
    }

    [TestMethod]
    public void UntrackedPropertiesNeedTheWorkshop()
    {
        Link();

        Assert.IsFalse(Edit(() => throw new WorkshopException("Steam isn't running")));

        StringAssert.Contains(_dialogs.Shown.Single().Config!.Message, "Steam isn't running");
    }

    [TestMethod]
    public void TrackedPropertiesCanBeEditedOffline()
    {
        Link(properties =>
        {
            properties.Title = "Resin Watch";
            properties.Description = "Tracks resin";
            properties.Visibility = WorkshopVisibility.Public;
            properties.Tags = ["Alyx Addon"];
            properties.RequiredItems = [];
        });
        _dialogs.OnPopupShown = popup => ((WorkshopPropertiesEditorViewModel)popup).Title = "Offline edit";
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit(() => throw new WorkshopException("Steam isn't running")));

        Assert.AreEqual("Offline edit", WorkshopProperties.Load(ContentPath)!.Title);
    }

    [TestMethod]
    public void WorkshopUpdateFailureKeepsLocalChanges()
    {
        Link(properties => properties.Title = "Local title");
        var connects = 0;
        _dialogs.OnPopupShown = popup => ((WorkshopPropertiesEditorViewModel)popup).Mismatches.Single().UpdateWorkshop = true;
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit(() => ++connects == 1 ? _client : throw new WorkshopException("Lost connection to Steam.")));

        Assert.AreEqual("Workshop Update Failed", _dialogs.Shown.Last().Config!.Title);
        Assert.AreEqual("Local title", WorkshopProperties.Load(ContentPath)!.Title);
    }

    [TestMethod]
    public void NewItemPropertiesAreSavedWithoutConnecting()
    {
        _dialogs.OnPopupShown = popup =>
        {
            var editor = (WorkshopPropertiesEditorViewModel)popup;
            Assert.IsTrue(editor.IsNewItem);
            editor.Title = "Resin Watch";
            editor.ChoosePreviewCommand.Execute(null);
        };
        _dialogs.PickFileResult = _fs.WriteFile("content/preview.png", "image");
        _dialogs.Respond(DialogResult.Primary);
        _dialogs.Respond(DialogResult.Primary);

        Assert.IsTrue(Edit());

        Assert.AreEqual(0, _connectCount);
        Assert.AreEqual("New Workshop Item", _dialogs.Shown[1].Config!.Title);
        var saved = WorkshopProperties.Load(ContentPath)!;
        Assert.IsFalse(saved.HasItem);
        Assert.AreEqual("Resin Watch", saved.Title);
        Assert.AreEqual("preview.png", saved.PreviewFile);
    }

    [TestMethod]
    public void ChangingTheLinkChecksTheItemAndKeepsProperties()
    {
        Link(properties => properties.Title = "Kept title");
        _store.Save(new PublishData(111, "Old test", "resin_watch", DateTimeOffset.FromUnixTimeSeconds(1000)));
        _dialogs.OnPopupShown = popup => ((ChangeLinkedItemViewModel)popup).Items.Single().IsSelected = true;
        _dialogs.Respond(DialogResult.Primary);

        var links = new WorkshopLinkService(_dialogs, _store);
        Assert.IsTrue(links.ChangeLinkAsync("resin_watch", ContentPath, () => _client, (_, _) => { }).Result);

        var saved = WorkshopProperties.Load(ContentPath)!;
        Assert.AreEqual(111UL, saved.ItemId);
        Assert.AreEqual("Kept title", saved.Title);
    }

    [TestMethod]
    public void ChangingTheLinkToAMissingItemAsksAgain()
    {
        Link();
        _client.Item = null;
        _dialogs.OnPopupShown = popup => ((ChangeLinkedItemViewModel)popup).OtherItemText = "999";
        _dialogs.Respond(DialogResult.Primary);
        _dialogs.Respond(DialogResult.None);
        _dialogs.Respond(DialogResult.None);

        var links = new WorkshopLinkService(_dialogs, _store);
        Assert.IsFalse(links.ChangeLinkAsync("resin_watch", ContentPath, () => _client, (_, _) => { }).Result);

        Assert.AreEqual("Can't Link Item", _dialogs.Shown[1].Config!.Title);
        Assert.AreEqual(ItemId, WorkshopProperties.Load(ContentPath)!.ItemId);
    }

    [TestMethod]
    public void LinkPickerStartsOnTheLinkedItem()
    {
        var picker = new ChangeLinkedItemViewModel(
            [new PublishData(111, "Release", "resin_watch", DateTimeOffset.Now), new PublishData(222, "Test", "resin_watch", DateTimeOffset.Now)],
            222);

        Assert.AreEqual(222UL, picker.SelectedItemId);

        picker.Items[0].IsSelected = true;
        Assert.AreEqual(111UL, picker.SelectedItemId);
        Assert.IsFalse(picker.Items[1].IsSelected);

        picker.OtherItemText = "https://steamcommunity.com/sharedfiles/filedetails/?id=333";
        Assert.AreEqual(333UL, picker.SelectedItemId);
        Assert.IsFalse(picker.Items[0].IsSelected);
    }

    [TestMethod]
    public void LinkPickerShowsAnItemLinkedById()
    {
        var picker = new ChangeLinkedItemViewModel([], 444);

        Assert.IsTrue(picker.UseOtherItem);
        Assert.AreEqual(444UL, picker.SelectedItemId);
    }
}
