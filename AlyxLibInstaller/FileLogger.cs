using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace AlyxLibInstaller
{
    /// <summary>
    /// Simple static logger for writing log messages to a persistent file.
    /// </summary>
    public static class FileLogger
    {
        private static readonly object _lock = new();
        private static readonly string _logDirectory;
        private static readonly string _logFilePath;
        private const int MaxLogFiles = 10;

        static FileLogger()
        {
            _logDirectory = PathHelper.LogsPath;
            Directory.CreateDirectory(_logDirectory);

            var date = DateTime.Now.ToString("yyyyMMdd");
            _logFilePath = Path.Combine(_logDirectory, $"log_{date}.txt");
        }

        /// <summary>
        /// Log a message to the log file with a timestamp.
        /// </summary>
        public static void Log(string message)
        {
            WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        /// <summary>
        /// Log a message with a specific log level.
        /// </summary>
        public static void Log(string message, string level)
        {
            WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        }

        /// <summary>
        /// Log an exception's message and full stack trace (including inner exceptions).
        /// </summary>
        public static void Log(Exception exception)
        {
            if (exception == null)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] [EXCEPTION] {FormatException(exception)}";
            WriteLine(logEntry);
        }

        public static void Log(Exception exception, string message)
        {
            if (exception == null)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] [EXCEPTION] {message} : {FormatException(exception)}";
            WriteLine(logEntry);
        }

        /// <summary>
        /// Writes a session separator with timestamp to the log file.
        /// </summary>
        public static void LogSessionStart()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var separator = $"========== Application started at {timestamp} ==========";
            WriteLine(separator);
        }

        /// <summary>
        /// Formats the exception and all inner exceptions recursively.
        /// </summary>
        private static string FormatException(Exception ex)
        {
            var msg = $"{ex.GetType()}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                msg += $"{Environment.NewLine}---> Inner Exception:{Environment.NewLine}{FormatException(ex.InnerException)}";
            }
            return msg;
        }

        private static void WriteLine(string line)
        {
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }

        /// <summary>
        /// Gets the current log file path.
        /// </summary>
        public static string LogFilePath => _logFilePath;

        /// <summary>
        /// Returns true if the number of log files exceeds the threshold.
        /// </summary>
        public static bool HasTooManyLogFiles()
        {
            try
            {
                var files = Directory.GetFiles(_logDirectory, "log_*.txt");
                return files.Length > MaxLogFiles;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the number of log files in the log directory.
        /// </summary>
        public static int LogFileCount
        {
            get
            {
                try
                {
                    return Directory.GetFiles(_logDirectory, "log_*.txt").Length;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Optionally, delete oldest log files to keep only the most recent MaxLogFiles.
        /// </summary>
        public static void CleanupOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(_logDirectory, "log_*.txt")
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.CreationTimeUtc)
                    .ToList();

                while (files.Count > MaxLogFiles)
                {
                    files[0].Delete();
                    files.RemoveAt(0);
                }
            }
            catch (Exception)
            {
                // Optionally log cleanup errors
            }
        }
    }
}