using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace GitCredentialManager.MSBuild
{
    public class GenerateWindowsAppManifest : Task
    {
        [Required]
        public string Version { get; set; }

        [Required]
        public string ApplicationName { get; set; }

        [Required]
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.Normal, "Creating application manifest file for '{0}'...", ApplicationName);

            string manifestDirectory = Path.GetDirectoryName(OutputFile);
            if (!Directory.Exists(manifestDirectory))
            {
                Directory.CreateDirectory(manifestDirectory);
            }

            // Construct a version object to ensure it is normalised to the "x.y.z.w" format
            // that Windows expects and requires.
            if (!System.Version.TryParse(Version, out Version version))
            {
                Log.LogError("Invalid version '{0}' specified for application manifest.", Version);
                return false;
            }

            // If we don't have a 4-component version, we need to add a revision component of 0,
            // otherwise Windows will reject the manifest.
            if (version.Revision < 0)
            {
                version = new Version(version.Major, version.Minor, version.Build, 0);
            }

            File.WriteAllText(
                OutputFile,
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<assembly manifestVersion=""1.0"" xmlns=""urn:schemas-microsoft-com:asm.v1"">
  <assemblyIdentity version=""{version.ToString(4)}"" name=""{ApplicationName}""/>
  <compatibility xmlns=""urn:schemas-microsoft-com:compatibility.v1"">
    <application>
      <!-- Windows 10 and Windows 11 -->
      <supportedOS Id=""{{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}}""/>
      <!-- Windows 8.1 -->
      <supportedOS Id=""{{1f676c76-80e1-4239-95bb-83d0f6d0da78}}""/>
      <!-- Windows 8 -->
      <supportedOS Id=""{{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}}""/>
      <!-- Windows 7 -->
      <supportedOS Id=""{{35138b9a-5d96-4fbd-8e2d-a2440225f93a}}""/>
    </application>
  </compatibility>
</assembly>
");

            return true;
        }
    }
}
