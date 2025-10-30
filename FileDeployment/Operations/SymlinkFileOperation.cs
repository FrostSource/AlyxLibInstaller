using FileDeployment.Exceptions;
using FileDeployment.Logging;

namespace FileDeployment.Operations
{
    [FileOperationAlias("Symlink")]
    public class SymlinkFileOperation : FileOperation, IFileOperationWithDestination
    {
        public VariableString Destination { get; set; } = new();

        //public override OperationType Type => OperationType.Symlink;

        public override void ExecuteWithoutRules()
        {
            //if (Source == null)
            //    throw new FileOperationException("Source must be set for SymlinkFileOperation");
            //if (Destination == null)
            //    throw new FileOperationException("Destination must be set for SymlinkFileOperation");

            //try
            //{
            // TODO: Implement symlink creation taking into account folder/file
            //Console.WriteLine($"Symlinking {Source} to {Destination}");

            //if (File.Exists(Destination) || Directory.Exists(Destination))
            //{
            //    if (ReplaceExistingFiles || (ReplaceExistingSymlinks && FileUtils.IsSymbolicLink(Destination)))
            //    {
            //        FileUtils.DeletePath(Destination);
            //    }
            //    else
            //    {
            //        //throw new FileAlreadyExistsException(this, $"Destination '{Destination}' already exists and will not be replaced.");
            //        //Log(new(this, $"Destination '{Destination}' already exists and will not be replaced.", ));
            //        Log(new(this, new Logging.FileWillNotBeReplaced() { Target=RuleTarget.Destination }));
            //        return; // Skip operation if we don't replace existing files or symlinks
            //    }
            //}

            if (ReplaceExistingSymlinks && FileUtils.IsSymbolicLink(Destination))
            {
                FileUtils.DeletePath(Destination);
            }

            // Ensure the parent directory of the destination exists
            FileUtils.CreateParentDirectories(Destination);

            if (Directory.Exists(Source))
            {
                Directory.CreateSymbolicLink(Destination, Source);
            }
            else if (File.Exists(Source))
            {
                File.CreateSymbolicLink(Destination, Source);
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
                return FileUtils.SymbolicLinkPointsTo(Destination, Source);
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
