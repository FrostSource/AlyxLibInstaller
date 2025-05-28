namespace FileDeployment
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class FileOperationAliasAttribute : Attribute
    {
        public string Alias { get; }
        public FileOperationAliasAttribute(string alias) => Alias = alias;
    }
}
