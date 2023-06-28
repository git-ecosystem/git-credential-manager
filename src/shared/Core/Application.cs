using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitCredentialManager.Commands;
using GitCredentialManager.Diagnostics;
using GitCredentialManager.Interop;

namespace GitCredentialManager
{
    public class Application : ApplicationBase, IConfigurableComponent
    {
        private readonly IHostProviderRegistry _providerRegistry;
        private readonly IConfigurationService _configurationService;
        private readonly IList<ProviderCommand> _providerCommands = new List<ProviderCommand>();
        private readonly List<IDiagnostic> _diagnostics = new List<IDiagnostic>();

        public Application(ICommandContext context)
            : this(context, new HostProviderRegistry(context), new ConfigurationService(context))
        {
        }

        internal Application(ICommandContext context,
                             IHostProviderRegistry providerRegistry,
                             IConfigurationService configurationService)
            : base(context)
        {
            EnsureArgument.NotNull(providerRegistry, nameof(providerRegistry));
            EnsureArgument.NotNull(configurationService, nameof(configurationService));

            _providerRegistry = providerRegistry;
            _configurationService = configurationService;

            _configurationService.AddComponent(this);
        }

        public void RegisterProvider(IHostProvider provider, HostProviderPriority priority)
        {
            _providerRegistry.Register(provider, priority);

            // If the provider is also a configurable component, add that to the configuration service
            if (provider is IConfigurableComponent configurableProvider)
            {
                _configurationService.AddComponent(configurableProvider);
            }

            // If the provider has custom commands to offer then create them here
            if (provider is ICommandProvider cmdProvider)
            {
                ProviderCommand providerCommand = cmdProvider.CreateCommand();
                _providerCommands.Add(providerCommand);
            }

            // If the provider exposes custom diagnostics use them
            if (provider is IDiagnosticProvider diagnosticProvider)
            {
                IEnumerable<IDiagnostic> providerDiagnostics = diagnosticProvider.GetDiagnostics();
                _diagnostics.AddRange(providerDiagnostics);
            }
        }

        protected override async Task<int> RunInternalAsync(string[] args)
        {
            var rootCommand = new RootCommand();
            var diagnoseCommand = new DiagnoseCommand(Context);

            // Add common options
            var noGuiOption = new Option<bool>("--no-ui", "Do not use graphical user interface prompts");
            rootCommand.AddGlobalOption(noGuiOption);

            void NoGuiOptionHandler(InvocationContext context)
            {
                if (context.ParseResult.HasOption(noGuiOption))
                {
                    Context.Settings.IsGuiPromptsEnabled = false;
                }
            }

            // Add standard commands
            rootCommand.AddCommand(new GetCommand(Context, _providerRegistry));
            rootCommand.AddCommand(new StoreCommand(Context, _providerRegistry));
            rootCommand.AddCommand(new EraseCommand(Context, _providerRegistry));
            rootCommand.AddCommand(new ConfigureCommand(Context, _configurationService));
            rootCommand.AddCommand(new UnconfigureCommand(Context, _configurationService));
            rootCommand.AddCommand(diagnoseCommand);

            // Add any custom provider commands
            foreach (ProviderCommand providerCommand in _providerCommands)
            {
                rootCommand.AddCommand(providerCommand);
            }

            // Add any custom provider diagnostic tests
            foreach (IDiagnostic providerDiagnostic in _diagnostics)
            {
                diagnoseCommand.AddDiagnostic(providerDiagnostic);
            }

            // Trace the current version, OS, runtime, and program arguments
            PlatformInformation info = PlatformUtils.GetPlatformInformation(Context.Trace2);
            Context.Trace.WriteLine($"Version: {Constants.GcmVersion}");
            Context.Trace.WriteLine($"Runtime: {info.ClrVersion}");
            Context.Trace.WriteLine($"Platform: {info.OperatingSystemType} ({info.CpuArchitecture})");
            Context.Trace.WriteLine($"OSVersion: {info.OperatingSystemVersion}");
            Context.Trace.WriteLine($"AppPath: {Context.ApplicationPath}");
            Context.Trace.WriteLine($"InstallDir: {Context.InstallationDirectory}");
            Context.Trace.WriteLine($"Arguments: {string.Join(" ", args)}");

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(OnException)
                .AddMiddleware(NoGuiOptionHandler)
                .Build();

            return await parser.InvokeAsync(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _providerRegistry?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnException(Exception ex, InvocationContext invocationContext)
        {
            if (ex is AggregateException aex)
            {
                aex.Handle(WriteException);
            }
            else
            {
                WriteException(ex);
            }

            invocationContext.ExitCode = -1;
        }

        private bool WriteException(Exception ex)
        {
            // Try and use a nicer format for some well-known exception types
            switch (ex)
            {
                case GitException gitEx:
                    Context.Streams.Error.WriteLine("fatal: {0} [{1}]", gitEx.Message, gitEx.ExitCode);
                    Context.Streams.Error.WriteLine(gitEx.GitErrorMessage);
                    break;
                case InteropException interopEx:
                    Context.Streams.Error.WriteLine("fatal: {0} [0x{1:x}]", interopEx.Message, interopEx.ErrorCode);
                    break;
                default:
                    Context.Streams.Error.WriteLine("fatal: {0}", ex.Message);
                    break;
            }

            // If tracing is enabled then also print the stack trace to stderr
            bool printStack = Context.Settings.GetTracingEnabled(out _);
            if (printStack)
                Context.Streams.Error.WriteLine(ex.StackTrace);

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

            IGitConfiguration config = Context.Git.GetConfiguration();

            // We are looking for the following to be set in the config:
            //
            // [credential]
            //     ...                # any number of helper entries (possibly none)
            //     helper =           # an empty value to reset/clear any previous entries (if applicable)
            //     helper = {appPath} # the expected executable value & directly following the empty value
            //     ...                # any number of helper entries (possibly none, but not the empty value '')
            //
            string[] currentValues = config.GetAll(configLevel, GitConfigurationType.Raw, helperKey).ToArray();

            // Try to locate an existing app entry with a blank reset/clear entry immediately preceding,
            // and no other blank empty/clear entries following (which effectively disable us).
            int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, appPath));
            int lastEmptyIndex = Array.FindLastIndex(currentValues, string.IsNullOrWhiteSpace);
            if (appIndex > 0 && string.IsNullOrWhiteSpace(currentValues[appIndex - 1]) && lastEmptyIndex < appIndex)
            {
                Context.Trace.WriteLine("Credential helper configuration is already set correctly.");
            }
            else
            {
                Context.Trace.WriteLine("Updating Git credential helper configuration...");

                // Clear any existing app entries in the configuration
                config.UnsetAll(configLevel, helperKey, Regex.Escape(appPath));

                // Add an empty value for `credential.helper`, which has the effect of clearing any helper value
                // from any lower-level Git configuration, then add a second value which is the actual executable path.
                config.Add(configLevel, helperKey, string.Empty);
                config.Add(configLevel, helperKey, appPath);
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

            IGitConfiguration config = Context.Git.GetConfiguration();

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

            string[] currentValues = config.GetAll(configLevel, GitConfigurationType.Raw, helperKey).ToArray();

            int appIndex = Array.FindIndex(currentValues, x => Context.FileSystem.IsSamePath(x, appPath));
            if (appIndex > -1)
            {
                // Check for the presence of a blank entry immediately preceding an app entry in the last position
                if (appIndex > 0 && appIndex == currentValues.Length - 1 &&
                    string.IsNullOrWhiteSpace(currentValues[appIndex - 1]))
                {
                    // Clear the blank entry
                    config.UnsetAll(configLevel, helperKey, Constants.RegexPatterns.Empty);
                }

                // Clear app entry
                string appEntryValue = currentValues[appIndex];
                config.UnsetAll(configLevel, helperKey, Regex.Escape(appEntryValue));
            }

            return Task.CompletedTask;
        }

        private string GetGitConfigAppPath()
        {
            string path = Context.ApplicationPath;

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
