using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace GitCredentialManager.MSBuild
{
    public class GetVersion : Task
    {
        [Required]
        public string VersionFile { get; set; }

        [Output]
        public string Version { get; set; }

        [Output]
        public string AssemblyVersion { get; set; }

        [Output]
        public string FileVersion { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, "Reading VERSION file...");
            string textVersion = File.ReadAllText(VersionFile);

            if (!System.Version.TryParse(textVersion, out System.Version fullVersion))
            {
                Log.LogError("Invalid version '{0}' specified.", textVersion);
                return false;
            }

            // System.Version names its version components as follows:
            // major.minor[.build[.revision]]
            // The main version number we use for GCM contains the first three
            // components.
            // The assembly and file version numbers contain all components, as
            // ommitting the revision portion from these properties causes
            // runtime failures on Windows.
            Version = $"{fullVersion.Major}.{fullVersion.Minor}.{fullVersion.Build}";
            AssemblyVersion = FileVersion = fullVersion.ToString();

            return true;
        }
    }
}
