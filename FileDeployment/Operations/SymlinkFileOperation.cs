using FileDeployment.Exceptions;
using System.Text.Json.Serialization;

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

            // TODO: Implement symlink creation taking into account folder/file
            Console.WriteLine($"Symlinking {Source} to {Destination}");
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
