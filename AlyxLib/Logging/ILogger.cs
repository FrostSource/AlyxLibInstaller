namespace AlyxLib.Logging;
public interface ILogger
{
    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="type"></param>
    /// <param name="severity"></param>
    void Log(string message, LogType type = LogType.Basic, LogSeverity severity = LogSeverity.Normal);

    /// <summary>
    /// Logs an error message with high severity by default.
    /// </summary>
    /// <param name="message">The error message to log</param>
    /// <param name="severity">The severity of the error</param>
    void LogError(string message, LogSeverity severity = LogSeverity.High) => Log(message, LogType.Error, severity);

    /// <summary>
    /// Logs a warning message with normal severity by default.
    /// </summary>
    /// <param name="message">The warning message to log</param>
    /// <param name="severity">The severity of the warning</param>
    void LogWarning(string message, LogSeverity severity = LogSeverity.Normal) => Log(message, LogType.Warning, severity);

    /// <summary>
    /// Logs a basic message with low severity.
    /// </summary>
    /// <param name="message"></param>
    void LogDetail(string message) => Log(message, LogType.Basic, LogSeverity.Low);

    /// <summary>
    /// Logs an info message with normal severity.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="severity"></param>
    void LogInfo(string message, LogSeverity severity = LogSeverity.Normal) => Log(message, LogType.Info, severity);
}
