using System.Reflection;

namespace GitCredentialManager;

public static class AssemblyUtils
{
    public static bool TryGetAssemblyVersion(out string version)
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            version = assemblyVersionAttribute is null
                ? assembly.GetName().Version.ToString()
                : assemblyVersionAttribute.InformationalVersion;
            return true;
        }
        catch
        {
            version = null;
            return false;
        }
    }
}
