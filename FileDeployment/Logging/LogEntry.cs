using FileDeployment.Rules;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Logging
{
    public class LogEntry
    {
        public LogEntryType Type { get; set; } = LogEntryType.Info;
        public FileOperation Operation { get; }
        public ValidationRule? Rule { get; }
        public string Source => Operation.Source.ToString();
        public string? Destination => (Operation as IFileOperationWithDestination)?.Destination?.ToString();
        public string Message { get; }
        public DateTime Timestamp { get; }
        public Exception? Exception { get; }

        public string? RelevantPath { get; }

        [MemberNotNullWhen(true, nameof(Exception))]
        public bool HasException => Exception != null;
        [MemberNotNullWhen(true, nameof(Destination))]
        public bool HasDestination => !string.IsNullOrEmpty(Destination);
        [MemberNotNullWhen(true, nameof(Rule))]
        public bool HasRule => Rule != null;

        public LogEntry(FileOperation operation, string message, Exception? exception = null)
        {
            Operation = operation;
            Message = message;
            Timestamp = DateTime.UtcNow;
            Exception = exception;
            Type = exception is null ? LogEntryType.Info : LogEntryType.Error;
        }

        /// <summary>
        /// Creates a log entry for an operation that failed with an exception.
        /// The operation type and source are derived from the provided operation instance.
        /// The message is set to the exception's message.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="exception"></param>
        public LogEntry(FileOperation operation, Exception exception)
        {
            Operation = operation;
            Message = exception.Message;
            Timestamp = DateTime.UtcNow;
            Exception = exception;
            Type = LogEntryType.Error;
        }

        /// <summary>
        /// Creates a log entry for an operation that failed due to a validation rule.
        /// The operation type and source are derived from the provided operation instance.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="rule"></param>
        /// <param name="message"></param>
        public LogEntry(FileOperation operation, ValidationRule rule, string message)
        {
            Operation = operation;
            Message = message;
            Timestamp = DateTime.UtcNow;
            Rule = rule;
            RelevantPath = rule.GetTargetPath(operation);
            Type = LogEntryType.Warning;
        }

        public LogEntry(FileOperation operation, ValidationRule rule, Exception? exception = null)
        {
            Operation = operation;
            Timestamp = DateTime.UtcNow;
            Rule = rule;
            Exception = exception;
            Message = exception?.Message ?? $"Validation rule failed ({rule.GetType().Name})";
            //Message = (rule is IValidationRuleWithValue rv) ? rv.Value : (exception?.Message ?? "Validation rule failed");
            RelevantPath = rule.GetTargetPath(operation);
            Type = exception is null ? LogEntryType.Warning : LogEntryType.Error;
        }


        public static LogEntry Success(FileOperation operation, string message)
        {
            return new LogEntry(operation, message) { Type = LogEntryType.Success };
        }

        public static LogEntry Success(FileOperation operation, ValidationRule rule, string message)
        {
            return new LogEntry(operation, rule, message) { Type = LogEntryType.Success };
        }

        public static LogEntry Error(FileOperation operation, string message, Exception? exception = null)
        {
            return new LogEntry(operation, message, exception) { Type = LogEntryType.Error };
        }

        public static LogEntry Error(FileOperation operation, ValidationRule rule, string message, Exception? exception = null)
        {
            return new LogEntry(operation, rule, exception) { Type = LogEntryType.Error };
        }

        public static LogEntry Warning(FileOperation operation, string message)
        {
            return new LogEntry(operation, message) { Type = LogEntryType.Warning };
        }

        public static LogEntry Warning(FileOperation operation, ValidationRule rule, Exception? exception = null)
        {
            return new LogEntry(operation, rule, exception) { Type = LogEntryType.Warning };
        }

        public static LogEntry Info(FileOperation operation, string message)
        {
            return new LogEntry(operation, message) { Type = LogEntryType.Info };
        }
    }
}
