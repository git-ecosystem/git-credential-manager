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

            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession &&
                TryFindHelperCommand(out string command, out string args))
            {
                var promptArgs = new StringBuilder(args);
                promptArgs.Append("oauth");

                if (!string.IsNullOrWhiteSpace(resource))
                {
                    promptArgs.AppendFormat(" --resource {0}", QuoteCmdArg(resource));
                }

                if ((modes & OAuthAuthenticationModes.Browser) != 0)
                {
                    promptArgs.Append(" --browser");
                }

                if ((modes & OAuthAuthenticationModes.DeviceCode) != 0)
                {
                    promptArgs.Append(" --device-code");
                }

                IDictionary<string, string> resultDict = await InvokeHelperAsync(command, promptArgs.ToString());

                if (!resultDict.TryGetValue("mode", out string responseMode))
                {
                    throw new Exception("Missing 'mode' in response");
                }

                switch (responseMode.ToLowerInvariant())
                {
                    case "browser":
                        return OAuthAuthenticationModes.Browser;

                    case "devicecode":
                        return OAuthAuthenticationModes.DeviceCode;

                    default:
                        throw new Exception($"Unknown mode value in response '{responseMode}'");
                }
            }
            else
            {
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

            // If we have a desktop session show the device code in a dialog
            if (Context.Settings.IsGuiPromptsEnabled && Context.SessionManager.IsDesktopSession &&
                TryFindHelperCommand(out string command, out string args))
            {
                var promptArgs = new StringBuilder(args);
                promptArgs.Append("device");
                promptArgs.AppendFormat(" --code {0} ", QuoteCmdArg(dcr.UserCode));
                promptArgs.AppendFormat(" --url {0}", QuoteCmdArg(dcr.VerificationUri.ToString()));

                var promptCts = new CancellationTokenSource();
                var tokenCts = new CancellationTokenSource();

                // Show the dialog with the device code but don't await its closure
                Task promptTask = InvokeHelperAsync(command, promptArgs.ToString(), null, promptCts.Token);

                // Start the request for an OAuth token but don't wait
                Task<OAuth2TokenResult> tokenTask = client.GetTokenByDeviceCodeAsync(dcr, tokenCts.Token);

                Task t = await Task.WhenAny(promptTask, tokenTask);

                // If the dialog was closed the user wishes to cancel the request
                if (t == promptTask)
                {
                    tokenCts.Cancel();
                }

                OAuth2TokenResult tokenResult;
                try
                {
                    tokenResult = await tokenTask;
                }
                catch (OperationCanceledException)
                {
                    throw new Exception("User canceled device code authentication");
                }

                // Close the dialog
                promptCts.Cancel();

                return tokenResult;
            }
            else
            {
                ThrowIfTerminalPromptsDisabled();

                string deviceMessage = $"To complete authentication please visit {dcr.VerificationUri} and enter the following code:" +
                                       Environment.NewLine +
                                       dcr.UserCode;
                Context.Terminal.WriteLine(deviceMessage);

                return await client.GetTokenByDeviceCodeAsync(dcr, CancellationToken.None);
            }
        }

        private bool TryFindHelperCommand(out string command, out string args)
        {
            return TryFindHelperCommand(
                Constants.EnvironmentVariables.GcmUiHelper,
                Constants.GitConfiguration.Credential.UiHelper,
                Constants.DefaultUiHelper,
                out command,
                out args);
        }
    }
}
