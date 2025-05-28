using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Logging
{
    public class LogEntry
    {
        public Type OperationType { get; }
        public string Source { get; }
        public string? Destination { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }
        public Exception? Exception { get; }
        public bool HasException => Exception != null;
        public bool HasDestination => !string.IsNullOrEmpty(Destination);

        public LogEntry(Type operationType, string source, string destination, string message, Exception? exception = null)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
            Message = message;
            Timestamp = DateTime.UtcNow;
            Exception = exception;
        }

        public LogEntry(FileOperation operation, string message, Exception? exception = null)
        {
            OperationType = operation.GetType();
            Source = operation.Source;
            Destination = (operation as IFileOperationWithDestination)?.Destination?.ToString();
            Message = message;
            Timestamp = DateTime.UtcNow;
            Exception = exception;
        }
    }
}
