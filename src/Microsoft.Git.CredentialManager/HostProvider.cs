using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface IHostProvider
    {
        string Name { get; }

        bool IsSupported(InputArguments input);

        string GetCredentialKey(InputArguments input);

        Task<GitCredential> CreateCredentialAsync(InputArguments input);

        bool IsCredentialStoredOnCreation { get; }
    }

    public abstract class HostProvider : IHostProvider
    {
        protected HostProvider(ICommandContext context)
        {
            Context = context;
        }

        protected ICommandContext Context { get; }

        public abstract string Name { get; }

        public abstract bool IsSupported(InputArguments input);

        public abstract string GetCredentialKey(InputArguments input);

        public abstract Task<GitCredential> CreateCredentialAsync(InputArguments input);

        public abstract bool IsCredentialStoredOnCreation { get; }
    }
}
