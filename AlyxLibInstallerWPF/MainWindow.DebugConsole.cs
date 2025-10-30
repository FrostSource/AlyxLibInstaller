using AlyxLibInstallerShared;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AlyxLibInstallerWPF;
public partial class MainWindow : Window
{
    private void AddHyperlink(Paragraph paragraph, string displayText, string linkTarget, bool isUrl)
    {
        var hyperlink = new Hyperlink();
        var run = new Run { Text = displayText };
        hyperlink.Inlines.Add(run);

        hyperlink.Click += async (s, e) =>
        {
            try
            {
                if (isUrl)
                {
                    if (Uri.TryCreate(linkTarget, UriKind.Absolute, out Uri? uriResult))
                    {
                        await Launcher.LaunchUriAsync(uriResult);
                    }
                }
                else if (Directory.Exists(linkTarget))
                {
                    await Launcher.LaunchFolderPathAsync(linkTarget);
                }
                else if (File.Exists(linkTarget))
                {
                    Process.Start("explorer.exe", $"/select,\"{linkTarget}\"");
                }
                else
                {
                    // Try to open the parent directory if the file doesn't exist
                    string parentDir = Path.GetDirectoryName(linkTarget);
                    if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                    {
                        await Launcher.LaunchFolderPathAsync(parentDir);
                    }
                }
            }
            catch (Exception)
            {
                // Optional: handle errors (log, show message, etc.)
            }
        };

        paragraph.Inlines.Add(hyperlink);
    }

    private string RefinePath(string candidate)
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

    private Paragraph CreateRichParagraph(string message, Color color)
    {
        var paragraph = new Paragraph();
        string urlPattern = @"\b(?:https?://|www\.)\S+\b";
        // Improved path pattern that captures more complete paths including spaces and extensions
        string pathPattern = @"[A-Za-z]:\\(?:[^<>:""/\\|?*\r\n'""]+\\)*[^<>:""/\\|?*\r\n'""]+";

        var matches = Regex.Matches(message, $"{urlPattern}|{pathPattern}", RegexOptions.IgnoreCase);
        int lastIndex = 0;

        if (matches.Count == 0)
        {
            paragraph.Inlines.Add(new Run { Text = message, Foreground = new SolidColorBrush(color) });
            return paragraph;
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
                    paragraph.Inlines.Add(new Run { Text = plainText, Foreground = new SolidColorBrush(color) });
                }

                AddHyperlink(paragraph, candidate, candidate, true);
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
                        paragraph.Inlines.Add(new Run { Text = plainText, Foreground = new SolidColorBrush(color) });
                    }

                    // Add hyperlink with full path displayed, but linking to valid path
                    AddHyperlink(paragraph, refinedPath, validPath, false);
                    lastIndex = match.Index + match.Length;
                }
                else
                {
                    // No valid path found, treat as normal text
                    if (match.Index > lastIndex)
                    {
                        string plainText = message.Substring(lastIndex, match.Index - lastIndex);
                        paragraph.Inlines.Add(new Run { Text = plainText, Foreground = new SolidColorBrush(color) });
                    }
                    paragraph.Inlines.Add(new Run { Text = candidate, Foreground = new SolidColorBrush(color) });
                    lastIndex = match.Index + match.Length;
                }
            }
        }

        // Add any remaining text
        if (lastIndex < message.Length)
        {
            paragraph.Inlines.Add(new Run { Text = message.Substring(lastIndex), Foreground = new SolidColorBrush(color) });
        }

        return paragraph;
    }

    public async void WriteToDebugConsole(string message, Color color)
    {
        // Automatically log every debug console message to file
        FileLogger.Log(message);

        var paragraph = CreateRichParagraph(message, color);

        //if (DebugConsole.Document.Blocks.Count > -1)
        //{
        paragraph.Margin = new Thickness(0, 4, 0, 0);
        //}

        DebugConsole.Document.Blocks.Add(paragraph);

        //TODO: Check if delay and dispatcher are needed in WPF
        await Task.Delay(20);

        Dispatcher.Invoke(() =>
        {
            DebugConsole.ScrollToEnd();
        });
    }

    private void Log(string message, string? verboseMessage, string colorResourceKey, bool verboseOnly = false)
    {
        bool verbose = SettingsManager.Settings.VerboseConsole;
        if (verboseOnly && !verbose)
            return;

        string msgToLog = (verbose && !string.IsNullOrEmpty(verboseMessage)) ? verboseMessage : message;

        //// WPF: Use basic color mapping, or default to Black
        //Color color = colorResourceKey switch
        //{
        //    "DebugConsoleNormalTextColor" => Colors.Black,
        //    "DebugConsoleVerboseTextColor" => Colors.Gray,
        //    "DebugConsoleVerboseWarningTextColor" => Colors.Orange,
        //    "DebugConsoleErrorTextColor" => Colors.Red,
        //    "DebugConsoleWarningTextColor" => Colors.Orange,
        //    "DebugConsoleInfoTextColor" => Colors.Blue,
        //    "DebugConsoleSuccessTextColor" => Colors.Green,
        //    _ => Colors.Black
        //};
        var color = (Color)Application.Current.Resources[colorResourceKey];

        WriteToDebugConsole(msgToLog, color);
    }

    // Normal message
    public void DebugConsoleMessage(string message) => Log(message, null, "DebugConsoleNormalTextColor");
    public void DebugConsoleMessage(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleNormalTextColor");

    // Verbose-only message
    public void DebugConsoleVerbose(string message) => Log(message, null, "DebugConsoleVerboseTextColor", verboseOnly: true);
    public void DebugConsoleVerboseWarning(string message) => Log(message, null, "DebugConsoleVerboseWarningTextColor", verboseOnly: true);
    public void DebugConsoleVerboseError(string message) => Log(message, null, "DebugConsoleVerboseErrorTextColor", verboseOnly: true);
    public void DebugConsoleVerboseInfo(string message) => Log(message, null, "DebugConsoleVerboseInfoTextColor", verboseOnly: true);
    public void DebugConsoleVerboseSuccess(string message) => Log(message, null, "DebugConsoleVerboseSuccessTextColor", verboseOnly: true);

    // Warning, Error, Info, Success
    public void DebugConsoleWarning(string message) => Log(message, null, "DebugConsoleWarningTextColor");
    public void DebugConsoleWarning(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleWarningTextColor");

    public void DebugConsoleError(string message) => Log(message, null, "DebugConsoleErrorTextColor");
    public void DebugConsoleError(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleErrorTextColor");

    public void DebugConsoleInfo(string message) => Log(message, null, "DebugConsoleInfoTextColor");
    public void DebugConsoleInfo(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleInfoTextColor");

    public void DebugConsoleSuccess(string message) => Log(message, null, "DebugConsoleSuccessTextColor");
    public void DebugConsoleSuccess(string message, string verboseMessage) => Log(message, verboseMessage, "DebugConsoleSuccessTextColor");

    // Exception
    public void DebugConsoleException(Exception ex)
    {
        Log($"{ex.GetType().Name} exception occurred! Check log for details {FileLogger.LogFilePath}", null, "DebugConsoleErrorTextColor");
        FileLogger.Log(ex);
    }
}
