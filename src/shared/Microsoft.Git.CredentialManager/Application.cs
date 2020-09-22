// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager.Commands;
using Microsoft.Git.CredentialManager.Interop;

namespace Microsoft.Git.CredentialManager
{
    public class Application : ApplicationBase, IConfigurableComponent
    {
        private readonly string _appPath;
        private readonly IHostProviderRegistry _providerRegistry;
        private readonly IConfigurationService _configurationService;

        public Application(ICommandContext context, string appPath)
            : this(context, new HostProviderRegistry(context), new ConfigurationService(context), appPath)
        {
        }

        internal Application(ICommandContext context,
                             IHostProviderRegistry providerRegistry,
                             IConfigurationService configurationService,
                             string appPath)
            : base(context)
        {
            EnsureArgument.NotNull(providerRegistry, nameof(providerRegistry));
            EnsureArgument.NotNull(configurationService, nameof(configurationService));
            EnsureArgument.NotNullOrWhiteSpace(appPath, nameof(appPath));

            _appPath = appPath;
            _providerRegistry = providerRegistry;
            _configurationService = configurationService;

            _configurationService.AddComponent(this);
        }

        public void RegisterProviders(params IHostProvider[] providers)
        {
            _providerRegistry.Register(providers);

            // Add any providers that are also configurable components to the configuration service
            foreach (IConfigurableComponent configurableProvider in providers.OfType<IConfigurableComponent>())
            {
                _configurationService.AddComponent(configurableProvider);
            }
        }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            string appName = Path.GetFileNameWithoutExtension(_appPath);

            // Construct all supported commands
            var commands = new CommandBase[]
            {
                new GetCommand(_providerRegistry),
                new StoreCommand(_providerRegistry),
                new EraseCommand(_providerRegistry),
                new ConfigureCommand(_configurationService),
                new UnconfigureCommand(_configurationService),
                new VersionCommand(),
                new HelpCommand(appName),
            };

            // Trace the current version and program arguments
            Context.Trace.WriteLine($"{Constants.GetProgramHeader()} '{string.Join(" ", args)}'");

            if (args.Length == 0)
            {
                Context.Streams.Error.WriteLine("Missing command.");
                HelpCommand.PrintUsage(Context.Streams.Error, appName);
                return -1;
            }

            foreach (var cmd in commands)
            {
                if (cmd.CanExecute(args))
                {
                    try
                    {
                        await cmd.ExecuteAsync(Context, args);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException ae)
                        {
                            ae.Handle(WriteException);
                        }
                        else
                        {
                            WriteException(e);
                        }

                        return -1;
                    }
                }
            }

            Context.Streams.Error.WriteLine("Unrecognized command '{0}'.", args[0]);
            HelpCommand.PrintUsage(Context.Streams.Error, appName);
            return -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _providerRegistry?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected bool WriteException(Exception ex)
        {
            // Try and use a nicer format for some well-known exception types
            switch (ex)
            {
                case InteropException interopEx:
                    Context.Streams.Error.WriteLine("fatal: {0} [0x{1:x}]", interopEx.Message, interopEx.ErrorCode);
                    break;
                default:
                    Context.Streams.Error.WriteLine("fatal: {0}", ex.Message);
                    break;
            }

            // Recurse to print all inner exceptions
            if (!(ex.InnerException is null))
            {
                WriteException(ex.InnerException);
            }

            return true;
        }

        #region IConfigurableComponent

        string IConfigurableComponent.Name => "Git Credential Manager";

        Task IConfigurableComponent.ConfigureAsync(
            IEnvironment environment, EnvironmentVariableTarget environmentTarget,
            IGit git, GitConfigurationLevel configurationLevel)
        {
            // NOTE: We currently only update the PATH in Windows installations and leave putting the GCM executable
            //       on the PATH on other platform to their installers.
            if (PlatformUtils.IsWindows())
            {
                string directoryPath = Path.GetDirectoryName(_appPath);
                if (!environment.IsDirectoryOnPath(directoryPath))
                {
                    Context.Trace.WriteLine("Adding application to PATH...");
                    environment.AddDirectoryToPath(directoryPath, environmentTarget);
                }
                else
                {
                    Context.Trace.WriteLine("Application is already on the PATH.");
                }
            }

            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string gitConfigAppName = GetGitConfigAppName();

            IGitConfiguration targetConfig = git.GetConfiguration(configurationLevel);

            /*
             * We are looking for the following to be considered already set:
             *
             * [credential]
             *     ...                         # any number of helper entries
             *     helper =                    # an empty value to reset/clear any previous entries
             *     helper = {gitConfigAppName} # the expected executable value in the last position & directly following the empty value
             *
             */

            string[] currentValues = targetConfig.GetRegex(helperKey, Constants.RegexPatterns.Any).ToArray();
            if (currentValues.Length < 2 ||
                !string.IsNullOrWhiteSpace(currentValues[currentValues.Length - 2]) ||    // second to last entry is empty
                currentValues[currentValues.Length - 1] != gitConfigAppName)              // last entry is the expected executable
            {
                Context.Trace.WriteLine("Updating Git credential helper configuration...");

                // Clear any existing entries in the configuration.
                targetConfig.UnsetAll(helperKey, Constants.RegexPatterns.Any);

                // Add an empty value for `credential.helper`, which has the effect of clearing any helper value
                // from any lower-level Git configuration, then add a second value which is the actual executable path.
                targetConfig.SetValue(helperKey, string.Empty);
                targetConfig.ReplaceAll(helperKey, Constants.RegexPatterns.None, gitConfigAppName);
            }
            else
            {
                Context.Trace.WriteLine("Credential helper configuration is already set correctly.");
            }


            return Task.CompletedTask;
        }

        Task IConfigurableComponent.UnconfigureAsync(
            IEnvironment environment, EnvironmentVariableTarget environmentTarget,
            IGit git, GitConfigurationLevel configurationLevel)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string gitConfigAppName = GetGitConfigAppName();

            IGitConfiguration targetConfig = git.GetConfiguration(configurationLevel);

            Context.Trace.WriteLine("Removing Git credential helper configuration...");

            // Clear any blank 'reset' entries
            targetConfig.UnsetAll(helperKey, Constants.RegexPatterns.Empty);

            // Clear GCM executable entries
            targetConfig.UnsetAll(helperKey, Regex.Escape(gitConfigAppName));

            // NOTE: We currently only update the PATH in Windows installations and leave removing the GCM executable
            //       on the PATH on other platform to their installers.
            // Remove GCM executable from the PATH
            if (PlatformUtils.IsWindows())
            {
                Context.Trace.WriteLine("Removing application from the PATH...");
                string directoryPath = Path.GetDirectoryName(_appPath);
                environment.RemoveDirectoryFromPath(directoryPath, environmentTarget);
            }

            return Task.CompletedTask;
        }

        private string GetGitConfigAppName()
        {
            const string gitCredentialPrefix = "git-credential-";

            string appName = Path.GetFileNameWithoutExtension(_appPath);
            if (appName != null && appName.StartsWith(gitCredentialPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return appName.Substring(gitCredentialPrefix.Length);
            }

            return _appPath;
        }

        #endregion
    }
}
