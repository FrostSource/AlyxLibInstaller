
namespace FileDeployment.Exceptions
{
    public class FileOperationException : Exception
    {
        public string? SourcePath { get; }
        public string? DestinationPath { get; }

        public FileOperationException(string message) : base(message) { }

        public FileOperationException(string message, Exception innerException)
            : base(message, innerException) { }

        public FileOperationException(string message, string? sourcePath, string? destinationPath)
            : base(message)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        public FileOperationException(string message, string? sourcePath, string? destinationPath, Exception innerException)
            : base(message, innerException)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }
    }
}
