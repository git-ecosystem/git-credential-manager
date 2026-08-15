using GitCredentialManager;

namespace Microsoft.ManagedApps
{
    /// <summary>
    /// Remembers which Microsoft Entra account was last used to authenticate against a given
    /// Microsoft Managed Apps environment host, so that subsequent silent token acquisitions
    /// (via MSAL) can be given the right account hint without prompting the user again.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>Microsoft.AzureRepos.AzureReposBindingManager</c>. Only a non-secret
    /// account/UPN value is stored, in Git configuration (not the secure credential store) -
    /// there is no long-lived credential to cache ourselves; the access token itself always
    /// comes fresh from MSAL's own cache.
    /// </remarks>
    public interface IManagedAppsBindingManager
    {
        /// <summary>
        /// Get the account last bound to the given environment host, or null if none exists.
        /// </summary>
        string GetAccount(string host);

        /// <summary>
        /// Bind an account to the given environment host.
        /// </summary>
        void SignIn(string host, string account);

        /// <summary>
        /// Remove any account binding for the given environment host.
        /// </summary>
        void SignOut(string host);
    }

    public class ManagedAppsBindingManager : IManagedAppsBindingManager
    {
        private readonly ITrace _trace;
        private readonly IGit _git;

        public ManagedAppsBindingManager(ICommandContext context) : this(context.Trace, context.Git) { }

        public ManagedAppsBindingManager(ITrace trace, IGit git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(git, nameof(git));

            _trace = trace;
            _git = git;
        }

        public string GetAccount(string host)
        {
            EnsureArgument.NotNullOrWhiteSpace(host, nameof(host));

            IGitConfiguration config = _git.GetConfiguration();

            if (config.TryGet(GitConfigurationLevel.Global, GitConfigurationType.Raw, GetAccountKey(host), out string account))
            {
                return account;
            }

            return null;
        }

        public void SignIn(string host, string account)
        {
            EnsureArgument.NotNullOrWhiteSpace(host, nameof(host));

            if (string.IsNullOrWhiteSpace(account))
            {
                _trace.WriteLine("Not recording an account binding - no account name is available.");
                return;
            }

            _trace.WriteLine($"Binding account '{account}' to Microsoft Managed Apps host '{host}'...");
            IGitConfiguration config = _git.GetConfiguration();
            config.Set(GitConfigurationLevel.Global, GetAccountKey(host), account);
        }

        public void SignOut(string host)
        {
            EnsureArgument.NotNullOrWhiteSpace(host, nameof(host));

            _trace.WriteLine($"Removing account binding for Microsoft Managed Apps host '{host}'...");
            IGitConfiguration config = _git.GetConfiguration();
            config.Unset(GitConfigurationLevel.Global, GetAccountKey(host));
        }

        private static string GetAccountKey(string host)
        {
            return $"{Constants.GitConfiguration.Credential.SectionName}.managedApps.{host}.account";
        }
    }
}
