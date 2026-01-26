using System.Collections.Generic;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Linux;

public class LinuxSettingsTests
{
    [LinuxFact]
    public void LinuxSettings_TryGetExternalDefault_CombinesFiles()
    {
        var env = new TestEnvironment();
        var git = new TestGit();
        var trace = new NullTrace();
        var fs = new TestFileSystem();

        var utf8 = EncodingEx.UTF8NoBom;

        fs.Directories = new HashSet<string>
        {
            "/",
            "/etc",
            "/etc/git-credential-manager",
            "/etc/git-credential-manager/config.d"
        };

        const string config1 = "core.overrideMe=value1";
        const string config2 = "core.overrideMe=value2";
        const string config3 = "core.overrideMe=value3";

        fs.Files = new Dictionary<string, byte[]>
        {
            ["/etc/git-credential-manager/config.d/01-first"] = utf8.GetBytes(config1),
            ["/etc/git-credential-manager/config.d/02-second"] = utf8.GetBytes(config2),
            ["/etc/git-credential-manager/config.d/03-third"] = utf8.GetBytes(config3),
        };

        var settings = new LinuxSettings(env, git, trace, fs);

        bool result = settings.TryGetExternalDefault(
            "core", null, "overrideMe", out string value);

        Assert.True(result);
        Assert.Equal("value3", value);
    }
}
