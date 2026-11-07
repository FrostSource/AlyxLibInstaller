using Source2HelperLibrary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AlyxLib.Workshop;

/// <summary>
/// Reads and writes the game\hlvr_addons\vpks folder, which holds one folder per published item
/// containing the item's VPK and publish_data.txt, the same layout the Workshop tools use.
/// </summary>
public partial class WorkshopItemStore(string vpksFolder)
{
    public const string PublishDataFileName = "publish_data.txt";
    private const string ReadableTimeFormat = "MM/dd/yyyy hh:mm:ss tt";

    public string VpksFolder { get; } = vpksFolder;

    /// <summary>
    /// Creates a store for the vpks folder of the installed game.
    /// </summary>
    public static WorkshopItemStore ForInstalledGame() => new(Path.Combine(HLA.GetAddonGameFolder(), "vpks"));

    public string GetItemFolder(ulong itemId) => Path.Combine(VpksFolder, itemId.ToString(CultureInfo.InvariantCulture));

    public string GetVpkPath(ulong itemId) => Path.Combine(GetItemFolder(itemId), $"{itemId}.vpk");

    /// <summary>
    /// Gets every published item with a readable publish_data.txt.
    /// </summary>
    public IReadOnlyList<PublishData> GetItems()
    {
        if (!Directory.Exists(VpksFolder))
            return [];

        var items = new List<PublishData>();
        foreach (var folder in Directory.EnumerateDirectories(VpksFolder))
        {
            if (!ulong.TryParse(Path.GetFileName(folder), out ulong itemId))
                continue;

            var file = Path.Combine(folder, PublishDataFileName);
            if (File.Exists(file) && TryParse(itemId, File.ReadAllText(file), out var data))
                items.Add(data);
        }

        return items;
    }

    /// <summary>
    /// Gets the items packed from an addon, most recently published first.
    /// An addon can have more than one item, e.g. a release and a test version.
    /// </summary>
    public IReadOnlyList<PublishData> GetItemsForAddon(string addonName) =>
        [.. GetItems()
            .Where(item => item.SourceFolder.Equals(addonName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.PublishTime)];

    /// <summary>
    /// Writes publish_data.txt for an item, creating its folder if needed.
    /// </summary>
    public void Save(PublishData data)
    {
        Directory.CreateDirectory(GetItemFolder(data.ItemId));
        File.WriteAllText(Path.Combine(GetItemFolder(data.ItemId), PublishDataFileName), Format(data));
    }

    public static string Format(PublishData data)
    {
        var readable = data.PublishTime.ToLocalTime().ToString(ReadableTimeFormat, CultureInfo.InvariantCulture);

        return new StringBuilder()
            .Append("\"publish_data\"\n{\n")
            .Append($"\t\"title\"\t\t\"{Escape(data.Title)}\"\n")
            .Append($"\t\"source_folder\"\t\t\"{Escape(data.SourceFolder)}\"\n")
            .Append($"\t\"publish_time\"\t\t\"{data.PublishTime.ToUnixTimeSeconds()}\"\n")
            .Append($"\t\"publish_time_readable\"\t\t\"{readable}\"\n")
            .Append("}\n")
            .ToString();
    }

    public static bool TryParse(ulong itemId, string text, out PublishData data)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in KeyValueRegex().Matches(text))
            values[match.Groups[1].Value] = Unescape(match.Groups[2].Value);

        values.TryGetValue("title", out var title);
        values.TryGetValue("source_folder", out var sourceFolder);
        values.TryGetValue("publish_time", out var publishTime);

        if (string.IsNullOrEmpty(sourceFolder))
        {
            data = null!;
            return false;
        }

        var time = long.TryParse(publishTime, out long seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : DateTimeOffset.MinValue;

        data = new PublishData(itemId, title ?? "", sourceFolder, time);
        return true;
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Unescape(string value) => value.Replace("\\\"", "\"").Replace("\\\\", "\\");

    [GeneratedRegex(@"""((?:[^""\\]|\\.)*)""\s+""((?:[^""\\]|\\.)*)""")]
    private static partial Regex KeyValueRegex();
}
