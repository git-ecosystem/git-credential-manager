using System.Threading.Tasks;

namespace GitCredentialManager.Tests.Objects
{
    public class TestHostProviderRegistry : IHostProviderRegistry
    {
        public IHostProvider Provider { get; set; }

        #region IHostProviderRegistry

        void IHostProviderRegistry.Register(IHostProvider hostProvider, HostProviderPriority priority)
        {
        }

        Task<IHostProvider> IHostProviderRegistry.GetProviderAsync(InputArguments input)
        {
            return Task.FromResult(Provider);
        }

        #endregion

        public void Dispose()
        {
            Provider?.Dispose();
        }
    }
}
