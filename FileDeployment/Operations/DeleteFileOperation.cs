using FileDeployment.Exceptions;
using System.Text.Json.Serialization;

namespace FileDeployment.Operations
{
    [FileOperationAlias("Delete")]
    public class DeleteFileOperation : FileOperation
    {
        //public override OperationType Type => OperationType.Delete;
        public override void ExecuteWithoutRules()
        {
            //if (Source == null)
            //    throw new FileOperationException("Source must be set for DeleteFileOperation");

            Console.WriteLine($"Deleting {Source}");
        }

        //public DeleteFileOperation(string source)
        //{
        //    Source = source;
        //}

        //[JsonConstructor]
        //public DeleteFileOperation(VariableString source)
        //{
        //    Source = source;
        //}
    }
}
