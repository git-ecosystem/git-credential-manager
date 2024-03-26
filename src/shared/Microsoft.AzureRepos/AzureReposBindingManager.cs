using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GitCredentialManager;

namespace Microsoft.AzureRepos
{
    /// <summary>
    /// Manages bindings of users and organizations for Azure Repos.
    /// </summary>
    public interface IAzureReposBindingManager
    {
        /// <summary>
        /// Get the binding for the given Azure DevOps organization.
        /// </summary>
        /// <param name="orgName">Organization name.</param>
        /// <returns>Binding for the organization, or null if no binding exists.</returns>
        AzureReposBinding GetBinding(string orgName);

        /// <summary>
        /// Bind a user to the given organization.
        /// </summary>
        /// <param name="orgName">Organization to bind the user to.</param>
        /// <param name="userName">User identifier to bind.</param>
        /// <param name="local">If true then bind local configuration, otherwise unbind global configuration.</param>
        /// <remarks>
        /// To prevent inheritance of a user binding at the global level, you can "bind" an organization
        /// to a special <paramref name="userName"/> value <see cref="AzureReposBinding.NoInherit"/>.
        /// <para/>
        /// The special value <see cref="AzureReposBinding.NoInherit"/> can be used as the <paramref name="userName"/>
        /// only when <paramref name="local"/> is true.
        /// </remarks>
        void Bind(string orgName, string userName, bool local);

        /// <summary>
        /// Unbind the given organization.
        /// </summary>
        /// <param name="orgName">Organization to unbind.</param>
        /// <param name="local">If true then unbind local configuration, otherwise unbind global configuration.</param>
        void Unbind(string orgName, bool local);

        /// <summary>
        /// Get all bindings to Azure DevOps organizations.
        /// </summary>
        /// <param name="orgName">Optional organization filter.</param>
        /// <returns>All organization bindings.</returns>
        IEnumerable<AzureReposBinding> GetBindings(string orgName = null);
    }

    public class AzureReposBinding
    {
        /// <summary>
        /// Do not inherit any higher-level binding.
        /// </summary>
        public const string NoInherit = "";

        public AzureReposBinding(string organization, string globalUserName, string localUserName)
        {
            Organization = organization;
            GlobalUserName = globalUserName;
            LocalUserName = localUserName;
        }

        public string Organization { get; }
        public string GlobalUserName { get; }
        public string LocalUserName { get; }
    }

    public class AzureReposBindingManager : IAzureReposBindingManager
    {
        private readonly ITrace _trace;
        private readonly IGit _git;

        public AzureReposBindingManager(ICommandContext context) : this(context.Trace, context.Git) { }

        public AzureReposBindingManager(ITrace trace, IGit git)
        {
            EnsureArgument.NotNull(trace, nameof(trace));
            EnsureArgument.NotNull(git, nameof(git));

            _trace = trace;
            _git = git;
        }

        public AzureReposBinding GetBinding(string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            string orgKey = GetOrgUserKey(orgName);

            IGitConfiguration config = _git.GetConfiguration();

            _trace.WriteLine($"Looking up organization binding for '{orgName}'...");

            string localUser = null;
            bool hasLocal = _git.IsInsideRepository() && // Can only check local config if we are inside a repository
                            config.TryGet(GitConfigurationLevel.Local, GitConfigurationType.Raw,
                                orgKey, out localUser);

            bool hasGlobal = config.TryGet(GitConfigurationLevel.Global, GitConfigurationType.Raw,
                orgKey, out string globalUser);

            if (hasLocal || hasGlobal)
            {
                return new AzureReposBinding(orgName, globalUser, localUser);
            }

            // No bound user
            return null;
        }

        public void Bind(string orgName, string userName, bool local)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            IGitConfiguration config = _git.GetConfiguration();

            string key = GetOrgUserKey(orgName);

            if (local)
            {
                _trace.WriteLine(userName == AzureReposBinding.NoInherit
                    ? $"Setting binding to 'do not inherit' for organization '{orgName}' in local repository..."
                    : $"Binding user '{userName}' to organization '{orgName}' in local repository...");

                if (_git.IsInsideRepository())
                {
                    config.Set(GitConfigurationLevel.Local, key, userName);
                }
                else
                {
                    _trace.WriteLine("Cannot set local configuration binding - not inside a repository!");
                }
            }
            else
            {
                EnsureArgument.NotNullOrWhiteSpace(userName, nameof(userName));

                _trace.WriteLine($"Binding user '{userName}' to organization '{orgName}' in global configuration...");
                config.Set(GitConfigurationLevel.Global, key, userName);
            }
        }

        public void Unbind(string orgName, bool local)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            IGitConfiguration config = _git.GetConfiguration();

            string key = GetOrgUserKey(orgName);

            if (local)
            {
                _trace.WriteLine($"Unbinding organization '{orgName}' in local repository...");
                if (_git.IsInsideRepository())
                {
                    config.Unset(GitConfigurationLevel.Local, key);
                }
                else
                {
                    _trace.WriteLine("Cannot set local configuration binding - not inside a repository!");
                }
            }
            else
            {
                _trace.WriteLine($"Unbinding organization '{orgName}' in global configuration...");
                config.Unset(GitConfigurationLevel.Global, key);
            }
        }

        public IEnumerable<AzureReposBinding> GetBindings(string orgName = null)
        {
            var globalUsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var localUsers  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            IGitConfiguration config = _git.GetConfiguration();

            string orgPrefix = $"{AzureDevOpsConstants.UrnOrgPrefix}/";

            bool ExtractUserBinding(GitConfigurationEntry entry, IDictionary<string, string> dict)
            {
                if (GitConfigurationKeyComparer.TrySplit(entry.Key, out _, out string scope, out _) &&
                    Uri.TryCreate(scope, UriKind.Absolute, out Uri uri) &&
                    uri.Scheme == AzureDevOpsConstants.UrnScheme && uri.AbsolutePath.StartsWith(orgPrefix))
                {
                    string entryOrgName = uri.AbsolutePath.Substring(orgPrefix.Length);
                    if (string.IsNullOrWhiteSpace(orgName) || StringComparer.OrdinalIgnoreCase.Equals(entryOrgName, orgName))
                    {
                        dict[entryOrgName] = entry.Value;
                    }
                }

                return true;
            }

            // Only enumerate local configuration if we are inside a repository
            if (_git.IsInsideRepository())
            {
                config.Enumerate(
                    GitConfigurationLevel.Local,
                    Constants.GitConfiguration.Credential.SectionName,
                    Constants.GitConfiguration.Credential.UserName,
                    entry => ExtractUserBinding(entry, localUsers));
            }

            config.Enumerate(
                GitConfigurationLevel.Global,
                Constants.GitConfiguration.Credential.SectionName,
                Constants.GitConfiguration.Credential.UserName,
                entry => ExtractUserBinding(entry, globalUsers));

            foreach (string org in globalUsers.Keys.Union(localUsers.Keys))
            {
                // NOT using the short-circuiting OR operator here on purpose - we need both branches to be evaluated
                if (globalUsers.TryGetValue(org, out string globalUser) | localUsers.TryGetValue(org, out string localUser))
                {
                    yield return new AzureReposBinding(org, globalUser, localUser);
                }
            }
        }

        private static string GetOrgUserKey(string orgName)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}:{2}/{3}.{4}",
                Constants.GitConfiguration.Credential.SectionName,
                AzureDevOpsConstants.UrnScheme, AzureDevOpsConstants.UrnOrgPrefix, orgName,
                "username"
            );
        }
    }

    public static class AzureReposUserManagerExtensions
    {
        /// <summary>
        /// Get the user that is bound to the specified Azure DevOps organization.
        /// </summary>
        /// <param name="bindingManager">Binding manager.</param>
        /// <param name="orgName">Organization name.</param>
        /// <returns>User identifier bound to the organization, or null if no such bound user exists.</returns>
        public static string GetUser(this IAzureReposBindingManager bindingManager, string orgName)
        {
            AzureReposBinding binding = bindingManager.GetBinding(orgName);
            if (binding is null || binding.LocalUserName == AzureReposBinding.NoInherit)
            {
                return null;
            }

            return binding.LocalUserName ?? binding.GlobalUserName;
        }

        /// <summary>
        /// Marks a user as 'signed in' to an Azure DevOps organization.
        /// </summary>
        /// <param name="bindingManager">Binding manager.</param>
        /// <param name="orgName">Organization name.</param>
        /// <param name="userName">User identifier to bind to this organization.</param>
        public static void SignIn(this IAzureReposBindingManager bindingManager, string orgName, string userName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));
            EnsureArgument.NotNullOrWhiteSpace(userName, nameof(userName));

            //
            // Try to bind the user to the organization.
            //
            //   A = User to sign-in
            //   B = Another user
            //   - = No user
            //
            //  Global | Local | -> | Global | Local
            // --------|-------|----|--------|-------
            //    -    |   -   | -> |   A    |   -
            //    -    |   A   | -> |   A    |   -
            //    -    |   B   | -> |   A    |   -
            //    A    |   -   | -> |   A    |   -
            //    A    |   A   | -> |   A    |   -
            //    A    |   B   | -> |   A    |   -
            //    B    |   -   | -> |   B    |   A
            //    B    |   A   | -> |   B    |   A
            //    B    |   B   | -> |   B    |   A
            //
            AzureReposBinding existingBinding = bindingManager.GetBinding(orgName);
            if (existingBinding?.GlobalUserName != null &&
                !StringComparer.OrdinalIgnoreCase.Equals(existingBinding.GlobalUserName, userName))
            {
                bindingManager.Bind(orgName, userName, local: true);
            }
            else
            {
                bindingManager.Bind(orgName, userName, local: false);
                bindingManager.Unbind(orgName, local: true);
            }
        }

        /// <summary>
        /// Marks a user as 'signed out' of an Azure DevOps organization.
        /// </summary>
        /// <param name="bindingManager">Binding manager.</param>
        /// <param name="orgName">Organization name.</param>
        public static void SignOut(this IAzureReposBindingManager bindingManager, string orgName)
        {
            EnsureArgument.NotNullOrWhiteSpace(orgName, nameof(orgName));

            //
            // Unbind the organization so we prompt the user to select a user on the next attempt.
            //
            //   U = User
            //   X = Do not inherit (valid in local only)
            //   - = No user
            //
            //  Global | Local | -> | Global | Local
            // --------|-------|----|--------|-------
            //    -    |   -   | -> |   -    |   -
            //    -    |   U   | -> |   -    |   -
            //    -    |   X   | -> |   -    |   -
            //    U    |   -   | -> |   U    |   X
            //    U    |   X   | -> |   U    |   X
            //    U    |   U   | -> |   U    |   X
            //
            AzureReposBinding existingBinding = bindingManager.GetBinding(orgName);
            if (existingBinding is null)
            {
                // Nothing to do!
            }
            else if (existingBinding.GlobalUserName is null)
            {
                bindingManager.Unbind(orgName, local: true);
            }
            else
            {
                bindingManager.Bind(orgName, AzureReposBinding.NoInherit, local: true);
            }
        }
    }
}
