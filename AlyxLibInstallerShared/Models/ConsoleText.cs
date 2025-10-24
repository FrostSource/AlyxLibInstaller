using AlyxLib.Logging;

namespace AlyxLibInstallerShared.Models;
public record ConsoleText(string Text, LogSeverity Severity) : ConsoleInline;