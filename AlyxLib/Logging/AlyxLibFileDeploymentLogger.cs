using FileDeployment.Logging;
using Source2HelperLibrary;

namespace AlyxLib.Logging;
public class AlyxLibFileDeploymentLogger(LocalAddon addon, ILogger? alyxlibLogger, bool verbose = false) : FileDeployment.Logging.IFileDeploymentLogger
{
    public LocalAddon Addon { get; } = addon;
    public ILogger? Logger { get; } = alyxlibLogger;
    public bool Verbose { get; } = verbose;

    private List<LogEntry> logEntries { get; } = new List<LogEntry>();

    public bool HasExceptions => logEntries.Any(e => e.HasException);

    /// <summary>
    /// Replaces all slashes in the path with the platform-specific directory separator and normalizes the path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        char sep = Path.DirectorySeparatorChar;

        // Replace both slash types with the platform separator
        return path.Replace('/', sep).Replace('\\', sep);
    }

    private static string FormatMessage(string message)
    {
        // If the message is not capitalized, capitalize the first letter
        if (char.IsLower(message[0]))
        {
            message = char.ToUpperInvariant(message[0]) + message[1..];
        }
        // If the message does not end with a period, add one
        if (!message.EndsWith('.'))
        {
            message += '.';
        }
        return message;
    }

    public void Log(LogEntry entry)
    {
        logEntries.Add(entry);

        string source = NormalizePath(entry.Source) ?? "";
        string destination = NormalizePath(entry.Destination) ?? "";
        if (Verbose)
        {
            source = Addon.GetContentFileRelativePath(source);
            destination = Addon.GetGameFileRelativePath(destination);
        }


        string message = (entry.Operation.Description != null)
            ? $"{entry.Operation.Description} : {entry.Message}"
            : entry.Message;

        if (entry.HasException)
        {
            //FileLogger.Log(entry.Exception); TODO
            message = entry.Exception.Message ?? message;
        }
        else if (entry.HasRule)
        {
            if (entry.Rule is FileWillNotBeReplaced rule)
            {
                message = $"File '{destination}' already exists and will not be replaced.";
            }
            else
            {
                message = (entry.Rule.Description != null)
                    ? $"{entry.Rule.Description} : {message}"
                    : message;
            }
        }

        switch (entry.Type)
        {
            case LogEntryType.Error:
                Logger?.Log(FormatMessage(message), LogType.Error, LogSeverity.High);
                break;

            case LogEntryType.Warning:
                Logger?.Log(FormatMessage(message), LogType.Warning, LogSeverity.Normal);
                break;

            case LogEntryType.Info:
                if (Verbose)
                    Logger?.Log(FormatMessage(message), LogType.Basic, LogSeverity.Low);
                break;

            case LogEntryType.Success:
                if (Verbose)
                    Logger?.Log(FormatMessage(message), LogType.Success, LogSeverity.Low);
                break;
        }
    }
}
