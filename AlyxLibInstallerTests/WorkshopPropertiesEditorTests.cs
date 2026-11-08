using AlyxLib.Workshop;
using AlyxLibInstallerShared.Services;
using AlyxLibInstallerShared.ViewModels;

namespace AlyxLibInstallerTests;

[TestClass]
public class WorkshopPropertiesEditorTests
{
    private const ulong ItemId = 3835625705;

    private TestFileSystem _fs = null!;
    private FakeDialogService _dialogs = null!;

    [TestInitialize]
    public void Setup()
    {
        _fs = new TestFileSystem();
        _dialogs = new FakeDialogService();
    }

    [TestCleanup]
    public void Cleanup() => _fs.Dispose();

    private static WorkshopItemDetails Remote(string title = "Resin Watch", string description = "Tracks resin", string[]? tags = null, ulong[]? requiredItems = null) =>
        new(ItemId, title, description, WorkshopVisibility.Public, tags ?? ["Alyx Addon", "Maps", "Short (less than 20m)", "Scripts & AI", "Made up tag"],
            requiredItems ?? [WorkshopProperties.AlyxLibItemId], "https://example.com/preview.png", DateTimeOffset.Now);

    private WorkshopPropertiesEditorViewModel Editor(WorkshopProperties? properties = null, WorkshopItemDetails? remote = null) =>
        new(properties ?? new WorkshopProperties { ItemId = ItemId }, _fs.Root, remote ?? Remote(), null, _dialogs);

    [TestMethod]
    public void UntrackedPropertiesStartFromTheWorkshop()
    {
        var editor = Editor();

        Assert.AreEqual("Resin Watch", editor.Title);
        Assert.AreEqual("Tracks resin", editor.Description);
        Assert.AreEqual(WorkshopVisibility.Public, editor.Visibility);
        Assert.IsTrue(editor.IsAlyxLibRequired);
        Assert.IsFalse(editor.HasMismatches);
        Assert.AreEqual("https://example.com/preview.png", editor.PreviewSource);
    }

    [TestMethod]
    public void UnknownTagsAndTheItemTypeTagAreKept()
    {
        var editor = Editor();

        Assert.AreEqual("Scripts & AI, Made up tag", editor.SelectedTagsSummary);
        CollectionAssert.AreEquivalent(new[] { "Alyx Addon", "Maps", "Short (less than 20m)", "Scripts & AI", "Made up tag" }, editor.Build().Tags);
    }

    [TestMethod]
    public void DifferencesListTheWorkshopValue()
    {
        var editor = Editor(new WorkshopProperties { ItemId = ItemId, Title = "Local title" });

        var mismatch = editor.Mismatches.Single();
        Assert.AreEqual(WorkshopFields.Title, mismatch.Field);
        Assert.AreEqual("Resin Watch", mismatch.WorkshopValue);
    }

    [TestMethod]
    public void UsingWorkshopVersionResolvesTheDifference()
    {
        var editor = Editor(new WorkshopProperties { ItemId = ItemId, Tags = ["Alyx Addon", "Maps"], RequiredItems = [] });
        Assert.AreEqual(2, editor.Mismatches.Count);

        editor.UseWorkshopVersionsCommand.Execute(null);

        Assert.IsFalse(editor.HasMismatches);
        Assert.AreEqual("Scripts & AI, Made up tag", editor.SelectedTagsSummary);
        Assert.IsTrue(editor.IsAlyxLibRequired);
    }

    [TestMethod]
    public void DifferencesAlreadyThereDontUpdateTheWorkshopUnlessChosen()
    {
        var editor = Editor(new WorkshopProperties { ItemId = ItemId, Title = "Local title" });
        Assert.IsFalse(editor.UpdateWorkshop);
        Assert.AreEqual(WorkshopFields.None, editor.FieldsToUpdate);
        editor.UpdateWorkshop = true;

        editor.Title = "Another local title";
        Assert.AreEqual(WorkshopFields.Title, editor.FieldsToUpdate);

        // Matching again means there's nothing to update
        editor.Title = "Resin Watch";
        Assert.AreEqual(WorkshopFields.None, editor.FieldsToUpdate);
    }

    [TestMethod]
    public void EditingMakesANewDifference()
    {
        var editor = Editor();

        editor.Visibility = WorkshopVisibility.Unlisted;

        Assert.AreEqual(WorkshopFields.Visibility, editor.Mismatches.Single().Field);
        Assert.AreEqual("Public", editor.Mismatches.Single().WorkshopValue);
        Assert.AreEqual("Visibility", editor.MismatchSummary);
        // Edits made here update the Workshop unless the user turns it off
        Assert.AreEqual(WorkshopFields.Visibility, editor.FieldsToUpdate);
    }

    [TestMethod]
    public void LinkedDescriptionFileReplacesTheTextAndCantBeOverwritten()
    {
        _fs.WriteFile("docs/description.txt", "From the file");
        _dialogs.PickFileResult = _fs.PathOf(@"docs\description.txt");
        var editor = Editor();

        editor.LinkDescriptionFileCommand.Execute(null);

        Assert.IsTrue(editor.HasDescriptionFile);
        Assert.AreEqual(@"docs\description.txt", editor.DescriptionFile);
        Assert.AreEqual("From the file", editor.CurrentDescription);
        var mismatch = editor.Mismatches.Single();
        Assert.AreEqual(WorkshopFields.Description, mismatch.Field);
        Assert.IsFalse(mismatch.CanUseWorkshopVersion);

        editor.UnlinkDescriptionFileCommand.Execute(null);

        Assert.IsFalse(editor.HasDescriptionFile);
        Assert.AreEqual("From the file", editor.Description);
        Assert.IsNull(editor.Build().DescriptionFile);
    }

    [TestMethod]
    public void MissingDescriptionFileCantBeSaved()
    {
        var editor = Editor(new WorkshopProperties { ItemId = ItemId, DescriptionFile = "missing.txt" });

        Assert.IsNotNull(editor.DescriptionFileError);
        StringAssert.Contains(editor.Validate(), "description file");
    }

    [TestMethod]
    public void RemovingAlyxLibShowsItCanBeAddedBack()
    {
        var editor = Editor();

        editor.RemoveRequiredItemCommand.Execute(editor.RequiredItems.Single());
        Assert.IsFalse(editor.IsAlyxLibRequired);

        editor.AddAlyxLibCommand.Execute(null);
        Assert.IsTrue(editor.IsAlyxLibRequired);
    }

    [TestMethod]
    public void RequiredItemsCanBeAddedByLink()
    {
        var editor = Editor();

        editor.NewRequiredItemText = "https://steamcommunity.com/sharedfiles/filedetails/?id=123456&searchtext=";
        editor.AddRequiredItemCommand.Execute(null);
        editor.NewRequiredItemText = "123456";
        editor.AddRequiredItemCommand.Execute(null);

        CollectionAssert.AreEqual(new ulong[] { WorkshopProperties.AlyxLibItemId, 123456 }, editor.Build().RequiredItems);
        Assert.AreEqual("", editor.NewRequiredItemText);
    }

    [TestMethod]
    public void InvalidRequiredItemsAreRejected()
    {
        var editor = Editor();

        editor.NewRequiredItemText = "not an item";
        editor.AddRequiredItemCommand.Execute(null);
        Assert.IsNotNull(editor.RequiredItemError);

        editor.NewRequiredItemText = ItemId.ToString();
        editor.AddRequiredItemCommand.Execute(null);
        StringAssert.Contains(editor.RequiredItemError, "itself");
        Assert.AreEqual(1, editor.RequiredItems.Count);
    }

    [TestMethod]
    public void TitleIsRequired()
    {
        var editor = Editor();
        editor.Title = "  ";

        Assert.AreEqual("Enter a title.", editor.Validate());
    }

    [TestMethod]
    public void OversizedPreviewIsConvertedWhenUploaded()
    {
        var preview = _fs.WriteFile("preview.png", new string('x', (int)PreviewImages.MaxBytes + 1));
        _dialogs.PickFileResult = preview;
        var editor = Editor();

        editor.ChoosePreviewCommand.Execute(null);

        Assert.IsTrue(editor.PreviewChanged);
        Assert.AreEqual("preview.png", editor.PreviewFile);
        Assert.IsNull(editor.Validate());
        StringAssert.Contains(editor.PreviewInfo, "compressed to a JPEG");
    }

    [TestMethod]
    public void OversizedGifSaysItLosesItsAnimation()
    {
        _dialogs.PickFileResult = _fs.WriteFile("preview.gif", new string('x', (int)PreviewImages.MaxBytes + 1));
        var editor = Editor();

        editor.ChoosePreviewCommand.Execute(null);

        StringAssert.Contains(editor.PreviewInfo, "only its first frame");
    }

    [TestMethod]
    public void SmallPreviewOnlyShowsItsSize()
    {
        _dialogs.PickFileResult = _fs.WriteFile("preview.png", "image");
        var editor = Editor();

        editor.ChoosePreviewCommand.Execute(null);

        Assert.AreEqual("preview.png, 5 bytes", editor.PreviewInfo);
    }

    [TestMethod]
    public void SingleChoiceTagsReplaceEachOther()
    {
        var editor = Editor();
        var addonType = editor.ChoiceTagGroups.Single(group => group.Name == "Addon Type");

        addonType.Choice = "Maps";
        addonType.Choice = "Developer Resources";

        CollectionAssert.Contains(editor.Build().Tags!.ToList(), "Developer Resources");
        CollectionAssert.DoesNotContain(editor.Build().Tags!.ToList(), "Maps");
        Assert.IsFalse(editor.CheckboxTagGroups.Any(group => group.Name == "Length"));

        // Like the Workshop tools, a choice can't be cleared
        CollectionAssert.DoesNotContain(addonType.Choices.ToList(), "None");
        addonType.Choice = null;
        Assert.AreEqual("Developer Resources", addonType.Choice);
    }

    [TestMethod]
    public void NewItemsRequireAlyxLibByDefault()
    {
        var editor = new WorkshopPropertiesEditorViewModel(new WorkshopProperties { ItemId = ItemId }, _fs.Root, null, "Offline", _dialogs);

        Assert.IsTrue(editor.IsAlyxLibRequired);
        Assert.AreEqual("Offline", editor.RemoteError);
        // Addon Type and Length start on their first option, like the Workshop tools
        CollectionAssert.AreEqual(new[] { WorkshopTags.AlyxAddon, "Maps", "Short (less than 20m)" }, editor.Build().Tags);
    }

    [TestMethod]
    [DataRow("3835625705", 3835625705UL)]
    [DataRow(" https://steamcommunity.com/sharedfiles/filedetails/?id=3329679071 ", 3329679071UL)]
    [DataRow("steamcommunity.com/workshop/filedetails/?l=english&id=42", 42UL)]
    public void ItemIdsAreReadFromIdsAndLinks(string text, ulong expected)
    {
        Assert.IsTrue(WorkshopLinks.TryParseItemId(text, out var itemId));
        Assert.AreEqual(expected, itemId);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("-5")]
    [DataRow("abc")]
    public void InvalidItemIdsAreRejected(string text)
    {
        Assert.IsFalse(WorkshopLinks.TryParseItemId(text, out _));
    }
}
