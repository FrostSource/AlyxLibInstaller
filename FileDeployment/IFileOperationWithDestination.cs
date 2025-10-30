namespace FileDeployment
{
    public interface IFileOperationWithDestination
    {
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VariableString Destination { get; set; }

        public bool SkipIfDestinationDoesNotExist => false;
    }
}
