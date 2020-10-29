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

        Task IConfigurableComponent.ConfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string appPath = GetGitConfigAppPath();

            GitConfigurationLevel configLevel = target == ConfigurationTarget.System
                    ? GitConfigurationLevel.System
                    : GitConfigurationLevel.Global;

            Context.Trace.WriteLine($"Configuring for config level '{configLevel}'.");

            IGitConfiguration config = Context.Git.GetConfiguration(configLevel);

            // We are looking for the following to be set in the config:
            //
            // [credential]
            //     ...                # any number of helper entries (possibly none)
            //     helper =           # an empty value to reset/clear any previous entries (if applicable)
            //     helper = {appPath} # the expected executable value & directly following the empty value
            //     ...                # any number of helper entries (possibly none)
            //
            string[] currentValues = config.GetRegex(helperKey, Constants.RegexPatterns.Any).ToArray();

            // Try to locate an existing app entry with a blank reset/clear entry immediately preceding
            int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, appPath));
            if (appIndex > 0 && string.IsNullOrWhiteSpace(currentValues[appIndex - 1]))
            {
                Context.Trace.WriteLine("Credential helper configuration is already set correctly.");
            }
            else
            {
                Context.Trace.WriteLine("Updating Git credential helper configuration...");

                // Clear any existing app entries in the configuration
                config.UnsetAll(helperKey, Regex.Escape(appPath));

                // Add an empty value for `credential.helper`, which has the effect of clearing any helper value
                // from any lower-level Git configuration, then add a second value which is the actual executable path.
                config.ReplaceAll(helperKey, Constants.RegexPatterns.None, string.Empty);
                config.ReplaceAll(helperKey, Constants.RegexPatterns.None, appPath);
            }

            return Task.CompletedTask;
        }

        Task IConfigurableComponent.UnconfigureAsync(ConfigurationTarget target)
        {
            string helperKey = $"{Constants.GitConfiguration.Credential.SectionName}.{Constants.GitConfiguration.Credential.Helper}";
            string appPath = GetGitConfigAppPath();

            GitConfigurationLevel configLevel = target == ConfigurationTarget.System
                    ? GitConfigurationLevel.System
                    : GitConfigurationLevel.Global;

            Context.Trace.WriteLine($"Unconfiguring for config level '{configLevel}'.");

            IGitConfiguration config = Context.Git.GetConfiguration(configLevel);

            // We are looking for the following to be set in the config:
            //
            // [credential]
            //     ...                 # any number of helper entries (possibly none)
            //     helper =            # an empty value to reset/clear any previous entries (if applicable)
            //     helper = {appPath} # the expected executable value & directly following the empty value
            //     ...                 # any number of helper entries (possibly none)
            //
            // We should remove the {appPath} entry, and any blank entries immediately preceding IFF there are no more entries following.
            //
            Context.Trace.WriteLine("Removing Git credential helper configuration...");

            string[] currentValues = config.GetRegex(helperKey, Constants.RegexPatterns.Any).ToArray();

            int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, appPath));
            if (appIndex > -1)
            {
                // Check for the presence of a blank entry immediately preceding an app entry in the last position
                if (appIndex > 0 && appIndex == currentValues.Length - 1 &&
                    string.IsNullOrWhiteSpace(currentValues[appIndex - 1]))
                {
                    // Clear the blank entry
                    config.UnsetAll(helperKey, Constants.RegexPatterns.Empty);
                }

                // Clear app entry
                config.UnsetAll(helperKey, Regex.Escape(appPath));
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

        private string GetGitConfigAppPath()
        {
            string path = _appPath;

            // On Windows we must use UNIX style path separators
            if (PlatformUtils.IsWindows())
            {
                path = path.Replace('\\', '/');
            }

            // We must escape escape characters like ' ', '(', and ')'
            return path
                .Replace(" ", "\\ ")
                .Replace("(", "\\(")
                .Replace(")", "\\)");;
        }

        #endregion
    }
}
