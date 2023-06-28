using System.CommandLine;
using System.Threading.Tasks;

namespace GitCredentialManager.Commands
{
    public abstract class ConfigurationCommandBase : Command
    {
        protected ConfigurationCommandBase(ICommandContext context, string name, string description, IConfigurationService configurationService)
            : base(name, description)
        {
            EnsureArgument.NotNull(context, nameof(context));
            EnsureArgument.NotNull(configurationService, nameof(configurationService));

            Context = context;
            ConfigurationService = configurationService;

            var system = new Option<bool>("--system", "Modify the system-wide Git configuration instead of the current user");
            AddOption(system);

            this.SetHandler(ExecuteAsync, system);
        }

        protected ICommandContext Context { get; }

        protected IConfigurationService ConfigurationService { get; }

        internal Task ExecuteAsync(bool system)
        {
            var target = system
                ? ConfigurationTarget.System
                : ConfigurationTarget.User;

            return ExecuteInternalAsync(target);
        }

        protected abstract Task ExecuteInternalAsync(ConfigurationTarget target);
    }

    public class ConfigureCommand : ConfigurationCommandBase
    {
        public ConfigureCommand(ICommandContext context, IConfigurationService configurationService)
            : base(context, "configure", "Configure Git Credential Manager as the Git credential helper", configurationService) { }

        protected override Task ExecuteInternalAsync(ConfigurationTarget target)
        {
            return ConfigurationService.ConfigureAsync(target);
        }
    }

    public class UnconfigureCommand : ConfigurationCommandBase
    {
        public UnconfigureCommand(ICommandContext context, IConfigurationService configurationService)
            : base(context, "unconfigure", "Unconfigure Git Credential Manager as the Git credential helper", configurationService) { }

        protected override Task ExecuteInternalAsync(ConfigurationTarget target)
        {
            return ConfigurationService.UnconfigureAsync(target);
        }
    }
}
