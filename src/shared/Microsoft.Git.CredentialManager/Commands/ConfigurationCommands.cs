// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Commands
{
    public abstract class ConfigurationCommandBase : VerbCommandBase
    {
        protected ConfigurationCommandBase(IConfigurationService configurationService)
        {
            EnsureArgument.NotNull(configurationService, nameof(configurationService));

            ConfigurationService = configurationService;
        }

        protected IConfigurationService ConfigurationService { get; }

        public override Task ExecuteAsync(ICommandContext context, string[] args)
        {
            var target = args.Any(x => StringComparer.OrdinalIgnoreCase.Equals("--system", x))
                ? ConfigurationTarget.System
                : ConfigurationTarget.User;

            return ExecuteInternalAsync(target);
        }

        protected abstract Task ExecuteInternalAsync(ConfigurationTarget target);
    }

    public class ConfigureCommand : ConfigurationCommandBase
    {
        public ConfigureCommand(IConfigurationService configurationService)
            : base(configurationService) { }

        protected override string Name => "configure";

        protected override Task ExecuteInternalAsync(ConfigurationTarget target)
        {
            return ConfigurationService.ConfigureAsync(target);
        }
    }

    public class UnconfigureCommand : ConfigurationCommandBase
    {
        public UnconfigureCommand(IConfigurationService configurationService)
            : base(configurationService) { }

        protected override string Name => "unconfigure";

        protected override Task ExecuteInternalAsync(ConfigurationTarget target)
        {
            return ConfigurationService.UnconfigureAsync(target);
        }
    }
}
