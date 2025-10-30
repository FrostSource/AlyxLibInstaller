using FileDeployment.Converters;
using FileDeployment.Logging;
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

        private string GetTemplatedString()
        {
            var template = File.ReadAllText(Source);

            // Replacements are formatted like string.Format in order, i.e. {0}, {1}, etc.
            for (int i = 0; i < Replacements.Count; i++)
            {
                template = template.Replace($"{{{i}}}", Replacements[i].ToString());
            }

            return template;
        }

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

            //try
            //{
            //Console.WriteLine($"Templating {Source} to {Destination} with replacements: {string.Join(", ", Replacements)}");

            //EnsurePathWritable(Destination);

            // Here you would typically read the source file, apply replacements, and write to destination
            var template = GetTemplatedString();

            // Ensure the parent directory of the destination exists
            FileUtils.CreateParentDirectories(Destination);

            File.WriteAllText(Destination, template);

            Log(LogEntry.Success(this, $"Templated file {Source} to {Destination} successfully"));
            //}
            //catch (Exception ex)
            //{
            //    Log(new(this, "Failed to template file", ex));
            //}
        }

        public override bool DeployedFileIsUnchanged()
        {
            try
            {
                string templated = GetTemplatedString();
                string deployedContent = File.ReadAllText(Destination);
                return string.Equals(templated, deployedContent, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
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
