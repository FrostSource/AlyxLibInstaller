namespace AlyxLib.Logging;

/// <summary>
/// The severity of a log message
/// </summary>
public enum LogSeverity
{
    /// <summary>
    /// Log severity, informational or verbose messages.
    /// </summary>
    Low,

    /// <summary>
    /// Normal severity, standard log messages.
    /// </summary>
    Normal,

    /// <summary>
    /// High severity, errors and warnings that require immediate attention.
    /// </summary>
    High
}
