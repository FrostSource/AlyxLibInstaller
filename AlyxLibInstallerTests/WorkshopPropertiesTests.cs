using AlyxLib.Workshop;

namespace AlyxLibInstallerTests;

[TestClass]
public class WorkshopPropertiesTests
{
    private TestFileSystem _fs = null!;

    [TestInitialize]
    public void Setup() => _fs = new TestFileSystem();

    [TestCleanup]
    public void Cleanup() => _fs.Dispose();

    private static WorkshopItemDetails Remote(
        string title = "Resin Watch",
        string description = "Line one\nLine two",
        WorkshopVisibility visibility = WorkshopVisibility.Public,
        string[]? tags = null,
        ulong[]? requiredItems = null) =>
        new(1, title, description, visibility, tags ?? ["Alyx Addon", "Scripts & AI"], requiredItems ?? [WorkshopProperties.AlyxLibItemId], "", DateTimeOffset.Now);

    private static WorkshopProperties Local() => new()
    {
        ItemId = 1,
        Title = "Resin Watch",
        Description = "Line one\r\nLine two  \r\n",
        Visibility = WorkshopVisibility.Public,
        Tags = ["scripts & ai", "Alyx Addon"],
        RequiredItems = [WorkshopProperties.AlyxLibItemId]
    };

    [TestMethod]
    public void SaveAndLoadRoundTrips()
    {
        var properties = Local();
        properties.DescriptionFile = "workshop_description.txt";
        properties.Save(_fs.Root);

        var loaded = WorkshopProperties.Load(_fs.Root)!;

        Assert.AreEqual(properties.ItemId, loaded.ItemId);
        Assert.AreEqual(properties.Title, loaded.Title);
        Assert.AreEqual(properties.DescriptionFile, loaded.DescriptionFile);
        Assert.AreEqual(properties.Visibility, loaded.Visibility);
        CollectionAssert.AreEqual(properties.Tags, loaded.Tags);
        CollectionAssert.AreEqual(properties.RequiredItems, loaded.RequiredItems);
        StringAssert.Contains(File.ReadAllText(WorkshopProperties.GetPath(_fs.Root)), "\"Public\"");
    }

    [TestMethod]
    public void UnlinkedAddonHasNoProperties()
    {
        Assert.IsNull(WorkshopProperties.Load(_fs.Root));
    }

    [TestMethod]
    public void LinkedDescriptionFileIsUsedInsteadOfText()
    {
        _fs.WriteFile("docs/description.txt", "From the file");
        var properties = new WorkshopProperties { Description = "From the text box", DescriptionFile = @"docs\description.txt" };

        Assert.AreEqual("From the file", properties.ReadDescription(_fs.Root));
    }

    [TestMethod]
    public void MissingDescriptionFileThrows()
    {
        var properties = new WorkshopProperties { DescriptionFile = "missing.txt" };

        Assert.ThrowsException<FileNotFoundException>(() => properties.ReadDescription(_fs.Root));
    }

    [TestMethod]
    public void MatchingPropertiesHaveNoDifferences()
    {
        var local = Local();

        // Line endings, trailing spaces and tag order and case don't count as changes
        Assert.AreEqual(WorkshopFields.None, WorkshopPropertyComparer.GetDifferences(local, local.Description, Remote()));
    }

    [TestMethod]
    public void UntrackedPropertiesAreNotCompared()
    {
        var local = new WorkshopProperties { ItemId = 1 };

        Assert.AreEqual(WorkshopFields.None, WorkshopPropertyComparer.GetDifferences(local, null, Remote(title: "Edited on the web")));
    }

    [TestMethod]
    public void EachChangedPropertyIsReported()
    {
        var local = Local();
        var remote = Remote(
            title: "Edited title",
            description: "Edited on the web",
            visibility: WorkshopVisibility.Unlisted,
            tags: ["Alyx Addon"],
            requiredItems: []);

        Assert.AreEqual(
            WorkshopFields.Title | WorkshopFields.Description | WorkshopFields.Visibility | WorkshopFields.Tags | WorkshopFields.RequiredItems,
            WorkshopPropertyComparer.GetDifferences(local, local.Description, remote));
    }
}
