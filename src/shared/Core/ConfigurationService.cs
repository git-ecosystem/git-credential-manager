using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace GitCredentialManager
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
        /// <param name="target">Configuration target.</param>
        Task ConfigureAsync(ConfigurationTarget target);

        /// <summary>
        /// Remove changes to the environment and Git configuration previously made with <see cref="ConfigureAsync"/>.
        /// </summary>
        /// <param name="target">Configuration target.</param>
        Task UnconfigureAsync(ConfigurationTarget target);
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

        /// <summary>
        /// Save the current configuration to a file.
        /// </summary>
        /// <param name="filePath">Path to the file where the configuration will be saved.</param>
        Task SaveConfigurationAsync(string filePath);

        /// <summary>
        /// Load the configuration from a file.
        /// </summary>
        /// <param name="filePath">Path to the file from which the configuration will be loaded.</param>
        Task LoadConfigurationAsync(string filePath);
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

        public async Task ConfigureAsync(ConfigurationTarget target)
        {
            foreach (IConfigurableComponent component in _components)
            {
                _context.Trace.WriteLine($"Configuring component '{component.Name}'...");
                _context.Streams.Error.WriteLine($"Configuring component '{component.Name}'...");
                await component.ConfigureAsync(target);
            }
        }

        public async Task UnconfigureAsync(ConfigurationTarget target)
        {
            foreach (IConfigurableComponent component in _components)
            {
                _context.Trace.WriteLine($"Unconfiguring component '{component.Name}'...");
                _context.Streams.Error.WriteLine($"Unconfiguring component '{component.Name}'...");
                await component.UnconfigureAsync(target);
            }
        }

        public async Task SaveConfigurationAsync(string filePath)
        {
            var configData = new Dictionary<string, object>();

            foreach (IConfigurableComponent component in _components)
            {
                var componentConfig = new Dictionary<string, object>
                {
                    { "Name", component.Name }
                };

                configData[component.Name] = componentConfig;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(configData, options);

            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task LoadConfigurationAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var configData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);

            foreach (var componentConfig in configData)
            {
                var componentName = componentConfig.Key;
                var component = _components.FirstOrDefault(c => c.Name == componentName);

                if (component != null)
                {
                    // Perform any necessary actions to apply the loaded configuration to the component
                }
            }
        }

        #endregion
    }
}
