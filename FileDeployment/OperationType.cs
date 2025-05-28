using System.Text.Json.Serialization;

namespace FileDeployment
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OperationType
    {
        Symlink,
        Copy,
        Template,
        Delete,
    }
}
