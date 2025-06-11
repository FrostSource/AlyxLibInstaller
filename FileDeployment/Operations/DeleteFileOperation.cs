using FileDeployment.Exceptions;
using System.Text.Json.Serialization;
using FileDeployment.Logging;

namespace FileDeployment.Operations
{
    [FileOperationAlias("Delete")]
    public class DeleteFileOperation : FileOperation
    {
        //public override OperationType Type => OperationType.Delete;
        public override bool SkipIfSourceDoesNotExist => true;
        public override void ExecuteWithoutRules()
        {
            //if (Source == null)
            //    throw new FileOperationException("Source must be set for DeleteFileOperation");

            //try
            //{
            if (!File.Exists(Source))
            {
                Log(new(this, $"Source file '{Source}' does not exist, nothing to delete."));
                return; // Nothing to delete, exit early
            }

            File.Delete(Source);
            //Console.WriteLine($"Deleting {Source}");
            Log(LogEntry.Success(this, $"Deleted file {Source} successfully"));
            //}
            //catch (Exception ex)
            //{
            //    Log(new(this, "Failed to delete file", ex));
            //}
        }

        public override bool DeployedFileIsUnchanged()
        {
            return false; // A delete operation does not have a deployed file to compare against, so it is always considered changed.
        }

        public override string GetDeploymentPath()
        {
            return string.Empty; // Delete operations do not deploy a file
        }

        //public override bool Execute()
        //{
        //    try
        //    {
        //        File.Delete(Source);
        //    }
        //    catch (Exception ex)
        //    {
        //        var fex = new FileOperationException($"Failed to delete source file '{Source}': {ex.Message}", this, Source, ex);
        //        Log(new(this, fex));
        //        return false;
        //    }

        //    Log(new(this, "Deleted file successfully"));
        //    return true;
        //}

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
