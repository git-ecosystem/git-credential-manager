// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Targets for performing configuration changes using the <see cref="IConfigurationService"/>.
    /// </summary>
    public enum ConfigurationTarget
    {
        /// <summary>
        /// Target configuration changes for the current user only.
        /// </summary>
        User,

        /// <summary>
        /// Target configuration changes for all users on the system.
        /// </summary>
        System,
    }

    /// <summary>
    /// Component that requires Git or environment configuration to work.
    /// </summary>
    public interface IConfigurableComponent
    {
        /// <summary>
        /// Name of the component that requires configuration.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Configure the environment and Git to work with this hosting provider.
        /// </summary>
        /// <param name="environment">Environment variables.</param>
        /// <param name="environmentTarget">Environment variable target to update.</param>
        /// <param name="git">Git object.</param>
        /// <param name="configurationLevel">Git configuration level to update.</param>
        Task ConfigureAsync(IEnvironment environment, EnvironmentVariableTarget environmentTarget,
            IGit git, GitConfigurationLevel configurationLevel);

        /// <summary>
        /// Remove changes to the environment and Git configuration previously made with <see cref="ConfigureAsync"/>.
        /// </summary>
        /// <param name="environment">Environment variables.</param>
        /// <param name="environmentTarget">Environment variable target to update.</param>
        /// <param name="git">Git object.</param>
        /// <param name="configurationLevel">Git configuration level to update.</param>
        Task UnconfigureAsync(IEnvironment environment, EnvironmentVariableTarget environmentTarget,
            IGit git, GitConfigurationLevel configurationLevel);
    }

    public interface IConfigurationService
    {
        /// <summary>
        /// Add a <see cref="IConfigurableComponent"/> to the collection of components that will be configured.
        /// </summary>
        /// <param name="component"></param>
        void AddComponent(IConfigurableComponent component);

        /// <summary>
        /// Configure all components.
        /// </summary>
        /// <param name="target">Target level to configure.</param>
        Task ConfigureAsync(ConfigurationTarget target);

        /// <summary>
        /// Unconfigure all components.
        /// </summary>
        /// <param name="target">Target level to unconfigure.</param>
        Task UnconfigureAsync(ConfigurationTarget target);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly ICommandContext _context;
        private readonly IList<IConfigurableComponent> _components = new List<IConfigurableComponent>();

        public ConfigurationService(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            _context = context;
        }

        #region IConfigurationService

        public void AddComponent(IConfigurableComponent component)
        {
            _components.Add(component);
        }

        public Task ConfigureAsync(ConfigurationTarget target) => RunAsync(target, true);

        public Task UnconfigureAsync(ConfigurationTarget target) => RunAsync(target, false);

        private async Task RunAsync(ConfigurationTarget target, bool configure)
        {
            GitConfigurationLevel configLevel;
            EnvironmentVariableTarget envTarget;
            switch (target)
            {
                case ConfigurationTarget.User:
                    configLevel = GitConfigurationLevel.Global;
                    envTarget   = EnvironmentVariableTarget.User;
                    break;

                case ConfigurationTarget.System:
                    configLevel = GitConfigurationLevel.System;
                    envTarget   = EnvironmentVariableTarget.Machine;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }

            foreach (IConfigurableComponent component in _components)
            {
                if (configure)
                {
                    _context.Trace.WriteLine($"Configuring component '{component.Name}'...");
                    _context.Streams.Error.WriteLine($"Configuring component '{component.Name}'...");
                    await component.ConfigureAsync(_context.Environment, envTarget, _context.Git, configLevel);
                }
                else
                {
                    _context.Trace.WriteLine($"Unconfiguring component '{component.Name}'...");
                    _context.Streams.Error.WriteLine($"Unconfiguring component '{component.Name}'...");
                    await component.UnconfigureAsync(_context.Environment, envTarget, _context.Git, configLevel);
                }
            }
        }

        #endregion
    }
}
