using System;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests;

public class SessionManagerTests
{
    [Fact]
    public void OpenBrowser_EscapedUri_PreservesEscaping()
    {
        const string expectedUrl = "https://example.com/sign-in/a%20b?q=c%20d";
        var manager = new CapturingSessionManager();

        manager.OpenBrowser(new Uri(expectedUrl));

        Assert.Equal(expectedUrl, manager.OpenedUrl);
    }

    private sealed class CapturingSessionManager : SessionManager
    {
        public CapturingSessionManager()
            : this(new TestFileSystem())
        {
        }

        private CapturingSessionManager(TestFileSystem fileSystem)
            : base(new NullTrace(), new TestEnvironment(fileSystem), fileSystem)
        {
        }

        public override bool IsDesktopSession => true;

        public string OpenedUrl { get; private set; }

        protected override void OpenBrowserInternal(string url)
        {
            OpenedUrl = url;
        }
    }
}
