using AlyxLib.Workshop;

namespace AlyxLibInstallerTests;

[TestClass]
public class WorkshopItemStoreTests
{
    private TestFileSystem _fs = null!;
    private WorkshopItemStore _store = null!;

    [TestInitialize]
    public void Setup()
    {
        _fs = new TestFileSystem();
        _store = new WorkshopItemStore(_fs.PathOf("vpks"));
    }

    [TestCleanup]
    public void Cleanup() => _fs.Dispose();

    [TestMethod]
    public void ParsesWorkshopToolsFile()
    {
        // Written by the Workshop tools
        const string text = "\"publish_data\"\r\n{\r\n\t\"title\"\t\t\"Resin Watch (Item Tracker)\"\r\n\t\"source_folder\"\t\t\"resin_watch\"\r\n" +
            "\t\"publish_time\"\t\t\"1774522209\"\r\n\t\"publish_time_readable\"\t\t\"03/26/2026 11:50:09 PM\"\r\n}\r\n";

        Assert.IsTrue(WorkshopItemStore.TryParse(3145397582, text, out var data));
        Assert.AreEqual("Resin Watch (Item Tracker)", data.Title);
        Assert.AreEqual("resin_watch", data.SourceFolder);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(1774522209), data.PublishTime);
    }

    [TestMethod]
    public void FormatRoundTripsQuotesAndBackslashes()
    {
        var data = new PublishData(1, "A \"quoted\" \\ title", "my_addon", DateTimeOffset.FromUnixTimeSeconds(1700000000));

        Assert.IsTrue(WorkshopItemStore.TryParse(1, WorkshopItemStore.Format(data), out var parsed));
        Assert.AreEqual(data, parsed);
    }

    [TestMethod]
    public void FileWithoutSourceFolderIsIgnored()
    {
        Assert.IsFalse(WorkshopItemStore.TryParse(1, "\"publish_data\"\n{\n\t\"title\"\t\t\"Untitled\"\n}", out _));
    }

    [TestMethod]
    public void FindsEveryItemForAnAddonNewestFirst()
    {
        _store.Save(new PublishData(100, "Holsters", "body_holsters", DateTimeOffset.FromUnixTimeSeconds(1000)));
        _store.Save(new PublishData(200, "Holsters Test", "body_holsters", DateTimeOffset.FromUnixTimeSeconds(2000)));
        _store.Save(new PublishData(300, "Resin Watch", "resin_watch", DateTimeOffset.FromUnixTimeSeconds(3000)));
        _fs.WriteFile("vpks/not_an_id/publish_data.txt", "\"source_folder\" \"body_holsters\"");
        Directory.CreateDirectory(_fs.PathOf("vpks/400"));

        var items = _store.GetItemsForAddon("BODY_HOLSTERS");

        CollectionAssert.AreEqual(new ulong[] { 200, 100 }, items.Select(item => item.ItemId).ToArray());
    }

    [TestMethod]
    public void MissingVpksFolderHasNoItems()
    {
        Assert.AreEqual(0, _store.GetItems().Count);
    }

    [TestMethod]
    public void PathsMatchWorkshopToolsLayout()
    {
        Assert.AreEqual(_fs.PathOf(@"vpks\123\123.vpk"), _store.GetVpkPath(123));
    }
}
