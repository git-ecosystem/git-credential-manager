using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCredentialManager.Authentication.OAuth;

namespace GitCredentialManager.Authentication
{
    [Flags]
    public enum OAuthAuthenticationModes
    {
        None        = 0,
        Browser     = 1 << 0,
        DeviceCode  = 1 << 1,

        All = Browser | DeviceCode
    }

    public interface IOAuthAuthentication
    {
        Task<OAuthAuthenticationModes> GetAuthenticationModeAsync(string resource, OAuthAuthenticationModes modes);

        Task<OAuth2TokenResult> GetTokenByBrowserAsync(OAuth2Client client, string[] scopes);

        Task<OAuth2TokenResult> GetTokenByDeviceCodeAsync(OAuth2Client client, string[] scopes);
    }

    public class OAuthAuthentication : AuthenticationBase, IOAuthAuthentication
    {
        public OAuthAuthentication(ICommandContext context)
            : base (context) { }

        public async Task<OAuthAuthenticationModes> GetAuthenticationModeAsync(
            string resource, OAuthAuthenticationModes modes)
        {
            EnsureArgument.NotNullOrWhiteSpace(resource, nameof(resource));

            ThrowIfUserInteractionDisabled();

            // Browser requires a desktop session!
            if (!Context.SessionManager.IsDesktopSession)
            {
                modes &= ~OAuthAuthenticationModes.Browser;
            }

            // We need at least one mode!
            if (modes == OAuthAuthenticationModes.None)
            {
                throw new ArgumentException(@$"Must specify at least one {nameof(OAuthAuthenticationModes)}", nameof(modes));
            }

            // If there is no mode choice to be made then just return that result
            if (modes == OAuthAuthenticationModes.Browser ||
                modes == OAuthAuthenticationModes.DeviceCode)
            {
                return modes;
            }

            ThrowIfTerminalPromptsDisabled();

            switch (modes)
            {
                case OAuthAuthenticationModes.Browser:
                    return OAuthAuthenticationModes.Browser;

                case OAuthAuthenticationModes.DeviceCode:
                    return OAuthAuthenticationModes.DeviceCode;

                default:
                    var menuTitle = $"Select an authentication method for '{resource}'";
                    var menu = new TerminalMenu(Context.Terminal, menuTitle);

                    TerminalMenuItem browserItem = null;
                    TerminalMenuItem deviceItem = null;

                    if ((modes & OAuthAuthenticationModes.Browser)    != 0) browserItem = menu.Add("Web browser");
                    if ((modes & OAuthAuthenticationModes.DeviceCode) != 0) deviceItem  = menu.Add("Device code");

                    // Default to the 'first' choice in the menu
                    TerminalMenuItem choice = menu.Show(0);

                    if (choice == browserItem) goto case OAuthAuthenticationModes.Browser;
                    if (choice == deviceItem)  goto case OAuthAuthenticationModes.DeviceCode;

                    throw new Exception();
            }
            
        }

        public async Task<OAuth2TokenResult> GetTokenByBrowserAsync(OAuth2Client client, string[] scopes)
        {
            ThrowIfUserInteractionDisabled();

            // We require a desktop session to launch the user's default web browser
            if (!Context.SessionManager.IsDesktopSession)
            {
                throw new InvalidOperationException("Browser authentication requires a desktop session");
            }

            var browserOptions = new OAuth2WebBrowserOptions();
            var browser = new OAuth2SystemWebBrowser(Context.Environment, browserOptions);
            var authCode = await client.GetAuthorizationCodeAsync(scopes, browser, CancellationToken.None);
            return await client.GetTokenByAuthorizationCodeAsync(authCode, CancellationToken.None);
        }

        public async Task<OAuth2TokenResult> GetTokenByDeviceCodeAsync(OAuth2Client client, string[] scopes)
        {
            ThrowIfUserInteractionDisabled();

            OAuth2DeviceCodeResult dcr = await client.GetDeviceCodeAsync(scopes, CancellationToken.None);

            ThrowIfTerminalPromptsDisabled();

            string deviceMessage = $"To complete authentication please visit {dcr.VerificationUri} and enter the following code:" +
                                    Environment.NewLine +
                                    dcr.UserCode;
            Context.Terminal.WriteLine(deviceMessage);

            return await client.GetTokenByDeviceCodeAsync(dcr, CancellationToken.None);
        }
    }
}
