using AlyxLib.Logging;
using AlyxLibInstallerShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Parsing;
public static class ConsoleParser
{
    public static ConsoleParagraph Parse(string message, LogSeverity severity)
    {
        var inlines = new List<ConsoleInline>();

        string urlPattern = @"\b(?:https?://|www\.)\S+\b";
        // Improved path pattern that captures more complete paths including spaces and extensions
        string pathPattern = @"[A-Za-z]:\\(?:[^<>:""/\\|?*\r\n'""]+\\)*[^<>:""/\\|?*\r\n'""]+";

        MatchCollection matches = Regex.Matches(message, $"{urlPattern}|{pathPattern}", RegexOptions.IgnoreCase);
        int lastIndex = 0;

        if (matches.Count == 0)
        {
            inlines.Add(new ConsoleText(message, severity));
            return new ConsoleParagraph(inlines);
        }

        foreach (Match match in matches)
        {
            string candidate = match.Value.Trim();

            // Check if this looks like a URL
            if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                // Add plain text before the match
                if (match.Index > lastIndex)
                {
                    string plainText = message.Substring(lastIndex, match.Index - lastIndex);
                    inlines.Add(new ConsoleText(plainText, severity));
                }

                inlines.Add(new ConsoleLink(candidate, candidate, true));
                lastIndex = match.Index + match.Length;
            }
            else
            {
                // It's a path - refine the candidate and find valid portion
                string refinedPath = RefinePath(candidate);
                string validPath = Utils.FindLongestValidPath(refinedPath);

                if (!string.IsNullOrEmpty(validPath))
                {
                    // Add plain text before the match
                    if (match.Index > lastIndex)
                    {
                        string plainText = message.Substring(lastIndex, match.Index - lastIndex);
                        inlines.Add(new ConsoleText(plainText, severity));
                    }

                    // Add hyperlink with full path displayed, but linking to valid path
                    inlines.Add(new ConsoleLink(refinedPath, validPath, false));
                    lastIndex = match.Index + match.Length;
                }
                else
                {
                    // No valid path found, treat as normal text
                    if (match.Index > lastIndex)
                    {
                        string plainText = message.Substring(lastIndex, match.Index - lastIndex);
                        inlines.Add(new ConsoleText(plainText, severity));
                    }
                    inlines.Add(new ConsoleText(candidate, severity));
                    lastIndex = match.Index + match.Length;
                }
            }
        }

        // Add any remaining text
        if (lastIndex < message.Length)
        {
            inlines.Add(new ConsoleText(message.Substring(lastIndex), severity));
        }

        return new ConsoleParagraph(inlines);
    }

    private static string RefinePath(string candidate)
    {
        // Remove newlines and excessive whitespace
        candidate = candidate.Replace("\r\n", " ").Replace("\n", " ");

        // Trim trailing punctuation that's unlikely to be part of a path
        candidate = candidate.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}', '"', '\'');

        // Trim whitespace
        candidate = candidate.Trim();

        // If path ends with a backslash followed by space and more text, trim after the backslash
        int lastBackslash = candidate.LastIndexOf('\\');
        if (lastBackslash > 0 && lastBackslash < candidate.Length - 1)
        {
            // Check if there's suspicious content after the last backslash (like multiple spaces)
            string afterBackslash = candidate.Substring(lastBackslash + 1);
            if (afterBackslash.StartsWith("  ")) // Multiple spaces suggest path ended
            {
                candidate = candidate.Substring(0, lastBackslash);
            }
        }

        return candidate;
    }
}