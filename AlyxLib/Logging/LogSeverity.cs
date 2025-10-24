using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
