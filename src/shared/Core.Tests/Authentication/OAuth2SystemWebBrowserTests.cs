using System;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Authentication;

public class OAuth2SystemWebBrowserTests
{
    [Fact]
    public void OAuth2SystemWebBrowser_UpdateRedirectUri_NonLoopback_ThrowsError()
    {
        var env = new TestEnvironment();
        var options = new OAuth2WebBrowserOptions();
        var browser = new OAuth2SystemWebBrowser(env, options);

        Assert.Throws<ArgumentException>(() => browser.UpdateRedirectUri(new Uri("http://example.com")));
    }

    [Theory]
    [InlineData("http://localhost:1234", "http://localhost:1234")]
    [InlineData("http://localhost:1234/", "http://localhost:1234/")]
    [InlineData("http://localhost:1234/oauth-callback", "http://localhost:1234/oauth-callback")]
    [InlineData("http://localhost:1234/oauth-callback/", "http://localhost:1234/oauth-callback/")]
    [InlineData("http://127.0.0.7:1234", "http://127.0.0.7:1234")]
    [InlineData("http://127.0.0.7:1234/", "http://127.0.0.7:1234/")]
    [InlineData("http://127.0.0.7:1234/oauth-callback", "http://127.0.0.7:1234/oauth-callback")]
    [InlineData("http://127.0.0.7:1234/oauth-callback/", "http://127.0.0.7:1234/oauth-callback/")]
    public void OAuth2SystemWebBrowser_UpdateRedirectUri_SpecificPort(string input, string expected)
    {
        var env = new TestEnvironment();
        var options = new OAuth2WebBrowserOptions();
        var browser = new OAuth2SystemWebBrowser(env, options);

        Uri actualUri = browser.UpdateRedirectUri(new Uri(input));

        Assert.Equal(expected, actualUri.OriginalString);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost/")]
    [InlineData("http://localhost/oauth-callback")]
    [InlineData("http://localhost/oauth-callback/")]
    [InlineData("http://127.0.0.7")]
    [InlineData("http://127.0.0.7/")]
    [InlineData("http://127.0.0.7/oauth-callback")]
    [InlineData("http://127.0.0.7/oauth-callback/")]
    public void OAuth2SystemWebBrowser_UpdateRedirectUri_AnyPort(string input)
    {
        var env = new TestEnvironment();
        var options = new OAuth2WebBrowserOptions();
        var browser = new OAuth2SystemWebBrowser(env, options);

        var inputUri = new Uri(input);
        Uri actualUri = browser.UpdateRedirectUri(inputUri);

        Assert.Equal(inputUri.Scheme, actualUri.Scheme);
        Assert.Equal(inputUri.Host, actualUri.Host);
        Assert.Equal(
            inputUri.GetComponents(UriComponents.Path, UriFormat.Unescaped),
            actualUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)
        );
        Assert.False(actualUri.IsDefaultPort);
    }
}
