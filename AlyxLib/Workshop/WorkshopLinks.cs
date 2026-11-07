using System.Globalization;
using System.Text.RegularExpressions;

namespace AlyxLib.Workshop;

public static partial class WorkshopLinks
{
    public static string GetItemUrl(ulong itemId) => $"https://steamcommunity.com/sharedfiles/filedetails/?id={itemId}";

    /// <summary>
    /// Reads a Workshop item ID typed as a number or pasted as an item page link.
    /// </summary>
    public static bool TryParseItemId(string? text, out ulong itemId)
    {
        itemId = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        var match = ItemLinkRegex().Match(text);
        if (match.Success)
            text = match.Groups[1].Value;

        return ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out itemId) && itemId != 0;
    }

    [GeneratedRegex(@"[?&]id=(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ItemLinkRegex();
}
