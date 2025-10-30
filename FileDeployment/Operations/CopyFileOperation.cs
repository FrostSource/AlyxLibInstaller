using FileDeployment.Logging;

namespace FileDeployment.Operations
{
    [FileOperationAlias("Copy")]
    public class CopyFileOperation : FileOperation, IFileOperationWithDestination
    {
        public VariableString Destination { get; set; } = new();

        //public override OperationType Type => OperationType.Copy;
        public override void ExecuteWithoutRules()
        {
            //if (Source == null)
            //    throw new FileOperationException("Source must be set for CopyFileOperation");
            //if (Destination == null)
            //    throw new FileOperationException("Destination must be set for CopyFileOperation");

            //try
            //{


            //EnsureDestinationWritable(Destination);

            // Ensure the parent directory of the destination exists
            FileUtils.CreateParentDirectories(Destination);

            // Source and Destination are VariableString, so they should be automatically formatted here
            //Console.WriteLine($"Copying {Source} to {Destination}");
            File.Copy(Source, Destination, true);
            //sourceStream.CopyTo(destinationStream);
            Log(LogEntry.Success(this, $"Copied file {Source} to {Destination} successfully"));

            //}
            //catch (Exception ex)
            //{
            //    Log(new(this, "Failed to copy file", ex));
            //}


        }

        public override bool DeployedFileIsUnchanged()
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                using FileStream fileStream = File.OpenRead(Source);
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.CopyTo(memoryStream);
                return FileUtils.FilesAreEqual(fileStream, Destination);
            }
            catch (Exception)
            {
                return false;
            }
        }

        //public CopyFileOperation(string source, string destination)
        //{
        //    Source = source;
        //    Destination = destination;
        //}

        //[JsonConstructor]
        //public CopyFileOperation(VariableString source, VariableString destination)
        //{
        //    Source = source;
        //    Destination = destination;
        //}
    }
}
