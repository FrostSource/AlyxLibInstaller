namespace FileDeployment
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ValidationRuleAliasAttribute : Attribute
    {
        public string Alias { get; }
        public ValidationRuleAliasAttribute(string alias) => Alias = alias;
    }
}
