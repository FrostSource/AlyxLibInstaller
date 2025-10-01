using FileDeployment.Exceptions;
using FileDeployment.Logging;
using System.Text.Json.Serialization;

namespace FileDeployment.Operations
{
    [FileOperationAlias("Hardlink")]
    public class HardlinkFileOperation : FileOperation, IFileOperationWithDestination
    {
        public VariableString Destination { get; set; } = new();

        //public override OperationType Type => OperationType.Symlink;
        
        public override void ExecuteWithoutRules()
        {

                if (ReplaceExistingSymlinks && FileUtils.IsSymbolicLink(Destination))
                {
                    FileUtils.DeletePath(Destination);
                }

                // Ensure the parent directory of the destination exists
                FileUtils.CreateParentDirectories(Destination);

                if (Directory.Exists(Source))
                {
                    throw new FileOperationException($"Source '{Source}' is a directory, a hardlink must be a file.", this, Source);
                }
                else if (File.Exists(Source))
                {
                    FileUtils.CreateHardLinkFile(Source, Destination);
                }
                else
                {
                    throw new FileOperationException($"Source '{Source}' does not exist.", this, Source);
                }

                Log(LogEntry.Success(this, $"Created symlink successfully"));
            //}
            //catch (Exception ex)
            //{
            //    Log(new(this, "Failed to create symlink", ex));
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

        //public SymlinkFileOperation(string source, string destination)
        //{
        //    Source = source;
        //    Destination = destination;
        //}

        //[JsonConstructor]
        //public SymlinkFileOperation(VariableString source, VariableString destination)
        //{
        //    Source = source;
        //    Destination = destination;
        //}
    }
}
