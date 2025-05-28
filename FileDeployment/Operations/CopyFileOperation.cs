using FileDeployment.Exceptions;
using System.Text.Json.Serialization;

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

            // Source and Destination are VariableString, so they should be automatically formatted here
            Console.WriteLine($"Copying {Source} to {Destination}");

            //File.Copy(Source, Destination, true);
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
