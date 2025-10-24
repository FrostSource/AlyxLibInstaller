using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLib.Logging;
public class ConsoleLogger : ILogger
{
    public void Log(string message, LogType type = LogType.Basic, LogSeverity severity = LogSeverity.Low)
    {
        Console.WriteLine($"[{severity} {type}] {message}");
    }
}
