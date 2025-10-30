namespace FileDeployment.Logging;

/// <summary>
/// Provides a basic logging implementation that writes log entries to the console.
/// </summary>
public class ConsoleLogger : IFileDeploymentLogger
{
    public void Log(LogEntry entry)
    {
        if (entry.HasRule)
        {
            Console.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Operation.GetType().Name} - {entry.Source} -> {entry.Destination ?? "N/A"}: {entry.Message} (Rule: {entry.Rule.GetType()?.Name})");
        }
        else
        {
            Console.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Operation.GetType().Name} - {entry.Source} -> {entry.Destination ?? "N/A"}: {entry.Message}");
        }
    }
}
