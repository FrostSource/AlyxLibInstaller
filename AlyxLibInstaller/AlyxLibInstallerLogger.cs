using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AlyxLib.Logging;

namespace AlyxLibInstaller;
internal class AlyxLibInstallerLogger : ILogger
{
    public void Log(string message, LogType type = LogType.Basic, LogSeverity severity = LogSeverity.Normal)
    {
        switch (type, severity)
        {
            // Standard messages
            case (LogType.Basic, LogSeverity.Low):
                App.DebugConsoleVerbose(message);
                break;
            case (LogType.Basic, _):
                App.DebugConsoleMessage(message);
                break;

            // Warning messages
            case (LogType.Warning, LogSeverity.Low):
                App.DebugConsoleVerboseWarning(message);
                break;
            case (LogType.Warning, _):
                App.DebugConsoleWarning(message);
                break;

            // Error messages
            case (LogType.Error, LogSeverity.Low):
                App.DebugConsoleVerboseError(message);
                break;
            case (LogType.Error, _):
                App.DebugConsoleError(message);
                break;

            case (LogType.Success, LogSeverity.Low):
                App.DebugConsoleVerbose(message);
                break;
            case (LogType.Success, _):
                App.DebugConsoleSuccess(message);
                break;
        }
    }
}
