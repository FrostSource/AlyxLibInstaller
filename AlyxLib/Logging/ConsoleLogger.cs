namespace AlyxLib.Logging;
public class ConsoleLogger : ILogger
{
    public void Log(string message, LogType type = LogType.Basic, LogSeverity severity = LogSeverity.Low)
    {
        Console.WriteLine($"[{severity} {type}] {message}");
    }
}
