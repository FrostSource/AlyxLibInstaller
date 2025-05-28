using FileDeployment.Converters;
using System.Text.Json.Serialization;

namespace FileDeployment
{
    [JsonConverter(typeof(VariableStringConverter))]
    public class VariableString : IManifestContext
    {
        public string RawString { get; set; }

        public DeploymentManifest? Manifest { get; private set; }

        public void SetManifestContext(DeploymentManifest manifest)
        {
            Manifest = manifest;
        }

        //private DeploymentManifest? Manifest;

        public VariableString()
        {
            RawString = "";
        }

        public VariableString(string value)
        {
            RawString = value;
        }

        //public void SetManifestContext(DeploymentManifest manifest)
        //{
        //    Manifest = manifest;
        //}

        public static implicit operator VariableString(string value) => new(value);
        public static implicit operator string(VariableString vs) => vs.ToString()?? "";

        public string Format()
        {
            if (Manifest == null)
                return RawString;

            string result = RawString;
            if (Manifest is null) throw new Exception("Manifest is null");
            if (Manifest.Variables is null) throw new Exception("Variables is null");
            // How do I get variables from a DeploymentManifest instance?
            foreach (var variable in Manifest.Variables)
            {
                result = result.Replace($"{{{variable.Key}}}", variable.Value());
            }
            return result;
        }

        public override string ToString()
        {
            // Test display to show VariableString
            //return $"$({RawString})";
            return Format();
        }
    }
}
