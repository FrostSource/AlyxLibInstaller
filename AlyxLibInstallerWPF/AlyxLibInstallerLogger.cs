using AlyxLib.Logging;

namespace AlyxLibInstallerWPF;
public class AlyxLibInstallerLogger(MainWindow window) : ILogger
{
    private readonly MainWindow window = window;
    public void Log(string message, LogType type = LogType.Basic, LogSeverity severity = LogSeverity.Normal)
    {
        switch (type, severity)
        {
            // Standard messages
            case (LogType.Basic, LogSeverity.Low):
                window.DebugConsoleVerbose(message);
                break;
            case (LogType.Basic, _):
                window.DebugConsoleMessage(message);
                break;

            // Warning messages
            case (LogType.Warning, LogSeverity.Low):
                window.DebugConsoleVerboseWarning(message);
                break;
            case (LogType.Warning, _):
                window.DebugConsoleWarning(message);
                break;

            // Error messages
            case (LogType.Error, LogSeverity.Low):
                window.DebugConsoleVerboseError(message);
                break;
            case (LogType.Error, _):
                window.DebugConsoleError(message);
                break;

            case (LogType.Success, LogSeverity.Low):
                window.DebugConsoleVerboseSuccess(message);
                break;
            case (LogType.Success, _):
                window.DebugConsoleSuccess(message);
                break;

            case (LogType.Info, LogSeverity.Low):
                window.DebugConsoleVerboseInfo(message);
                break;
            case (LogType.Info, _):
                window.DebugConsoleInfo(message);
                break;
        }
    }
}
