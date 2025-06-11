
namespace FileDeployment.Exceptions
{
    public class FileOperationException : Exception
    {
        //public string? SourcePath { get; }
        //public string? DestinationPath { get; }
        /// <summary>
        /// Gets the path of the file that caused the problem, if applicable.
        /// </summary>
        public string? ProblemFilePath { get; }
        public FileOperation? Operation { get; }

        //public FileOperationException(string message) : base(message) { }

        public FileOperationException(string message, FileOperation operation, string problemFilePath, Exception innerException)
            : base(message, innerException)
        {
            Operation = operation;
            ProblemFilePath = problemFilePath;
        }

        public FileOperationException(string message, FileOperation operation, string problemFilePath)
            : base(message)
        {
            Operation = operation;
            ProblemFilePath = problemFilePath;
        }

        //public FileOperationException(FileOperation operation, string message)
        //    : base(message)
        //{
        //    SourcePath = operation.Source?.ToString();
        //    DestinationPath = operation is IFileOperationWithDestination destOp ? destOp.Destination?.ToString() : null;
        //}

        //public FileOperationException(string message, string? sourcePath, string? destinationPath)
        //    : base(message)
        //{
        //    SourcePath = sourcePath;
        //    DestinationPath = destinationPath;
        //}

        //public FileOperationException(string message, string? sourcePath, string? destinationPath, Exception innerException)
        //    : base(message, innerException)
        //{
        //    SourcePath = sourcePath;
        //    DestinationPath = destinationPath;
        //}
    }
}
