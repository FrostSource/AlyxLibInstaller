using FileDeployment.Exceptions;
using FileDeployment.Converters;
using System.Text.Json.Serialization;

namespace FileDeployment.Operations
{
    [FileOperationAlias("Template")]
    public class TemplateFileOperation : FileOperation, IFileOperationWithDestination
    {
        public VariableString Destination { get; set; } = new();

        //public override OperationType Type => OperationType.Template;

        [JsonConverter(typeof(VariableStringListConverter))]
        public List<VariableString> Replacements { get; set; } = [];

        public override void ExecuteWithoutRules()
        {
            //if (Source == null)
            //    throw new FileOperationException("Source must be set for TemplateFileOperation");
            //if (Destination == null)
            //    throw new FileOperationException("Destination must be set for TemplateFileOperation");

            //if (!File.Exists(Source))
            //    // Should be a warning
            //    throw new FileOperationException($"Source file '{Source}' does not exist");

            //var template = File.ReadAllText(Source);
            // TODO template stuff

            Console.WriteLine($"Templating {Source} to {Destination} with replacements: {string.Join(", ", Replacements)}");
        }

        public override void SetManifestContext(DeploymentManifest manifest)
        {
            base.SetManifestContext(manifest);
            Replacements.ForEach(r => r.SetManifestContext(manifest));
        }

        //public TemplateFileOperation(VariableString source, VariableString destination, IEnumerable<VariableString>? replacements = null)
        //{
        //    Source = source;
        //    Destination = destination;
        //    Replacements = replacements?.ToList() ?? [];
        //}

        //public TemplateFileOperation(string source, string destination, IEnumerable<string>? replacements = null)
        //{
        //    Source = source;
        //    Destination = destination;
        //    Replacements = replacements?.Select(r => new VariableString(r)).ToList() ?? [];
        //}

        //[JsonConstructor]
        //private TemplateFileOperation(VariableString source, VariableString destination, List<VariableString>? replacements = null)
        //{
        //    Source = source;
        //    Destination = destination;
        //    Replacements = replacements ?? [];
        //}
    }
}
