using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager
{
    public interface IHostProviderRegistry
    {
        void Register(params IHostProvider[] hostProviders);

        IHostProvider GetProvider(InputArguments input);
    }

    public class HostProviderRegistry : IHostProviderRegistry
    {
        private readonly List<IHostProvider> _hostProviders = new List<IHostProvider>();

        public void Register(params IHostProvider[] hostProviders)
        {
            if (hostProviders == null)
            {
                throw new ArgumentNullException(nameof(hostProviders));
            }

            _hostProviders.AddRange(hostProviders);
        }

        public IHostProvider GetProvider(InputArguments input)
        {
            var provider = _hostProviders.FirstOrDefault(x => x.IsSupported(input));

            if (provider == null)
            {
                throw new Exception("No host provider available to service this request.");
            }

            return provider;
        }
    }
}
