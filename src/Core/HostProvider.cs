using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents a particular Git hosting service and provides for the creation of credentials to access the remote.
    /// </summary>
    public interface IHostProvider : IDisposable
    {
        /// <summary>
        /// Unique identifier of the hosting provider.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Name of the hosting provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Supported authority identifiers.
        /// </summary>
        IEnumerable<string> SupportedAuthorityIds { get; }

        /// <summary>
        /// Determine if the <see cref="GitRequest"/> is recognized by this particular Git hosting provider.
        /// </summary>
        /// <param name="request">Git credential request.</param>
        /// <returns>True if the provider supports the Git credential request, false otherwise.</returns>
        bool IsSupported(GitRequest request);

        /// <summary>
        /// Determine if the <see cref="HttpResponseMessage"/> identifies a recognized Git hosting provider.
        /// </summary>
        /// <param name="response">Response message of an endpoint query.</param>
        /// <returns>True if the provider supports the host provider at the endpoint, false otherwise.</returns>
        bool IsSupported(HttpResponseMessage response);

        /// <summary>
        /// Get a credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="request">Git credential request.</param>
        /// <returns>A credential Git can use to authenticate to the remote repository.</returns>
        Task<GetCredentialResult> GetCredentialAsync(GitRequest request);

        /// <summary>
        /// Store a credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="request">Git credential request.</param>
        Task StoreCredentialAsync(GitRequest request);

        /// <summary>
        /// Erase a stored credential for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="request">Git credential request.</param>
        Task EraseCredentialAsync(GitRequest request);
    }

    public class GetCredentialResult
    {
        public GetCredentialResult(ICredential credential)
        {
            Credential = credential;
        }

        public ICredential Credential { get; set; }
        public IDictionary<string, string> AdditionalProperties { get; set; }
            = new  Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Represents a Git hosting provider where credentials can be stored and recalled in/from the Operating System's
    /// secure credential store.
    /// </summary>
    public abstract class HostProvider : DisposableObject, IHostProvider
    {
        protected HostProvider(ICommandContext context)
        {
            Context = context;
        }

        /// <summary>
        /// The current command execution context.
        /// </summary>
        protected ICommandContext Context { get; }

        public abstract string Id { get; }

        public abstract string Name { get; }

        public virtual IEnumerable<string> SupportedAuthorityIds => Enumerable.Empty<string>();

        public abstract bool IsSupported(GitRequest request);

        public virtual bool IsSupported(HttpResponseMessage response)
        {
            return false;
        }

        /// <summary>
        /// Return a string that uniquely identifies the service that a credential should be stored against.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This key forms part of the identifier used to retrieve and store credentials from the OS secure
        /// credential storage system. It is important the returned value is stable over time to avoid any
        /// potential re-authentication requests.
        /// </para>
        /// <para>
        /// The default implementation returns the absolute URI formed by from the <see cref="GitRequest"/>
        /// without any userinfo component. Any trailing slashes are trimmed.
        /// </para>
        /// </remarks>
        /// <param name="request">Git credential request.</param>
        /// <returns>Credential service name.</returns>
        public virtual string GetServiceName(GitRequest request)
        {
            // By default we assume the service name will be the absolute URI based on the
            // request arguments from Git, without any userinfo part.
            return request.GetRemoteUri(includeUser: false).AbsoluteUri.TrimEnd('/');
        }

        /// <summary>
        /// Create a new credential used for accessing the remote Git repository on this hosting service.
        /// </summary>
        /// <param name="request">Git credential request.</param>
        /// <returns>A credential Git can use to authenticate to the remote repository.</returns>
        public abstract Task<ICredential> GenerateCredentialAsync(GitRequest request);

        public virtual async Task<GetCredentialResult> GetCredentialAsync(GitRequest request)
        {
            // Try and locate an existing credential in the OS credential store
            string service = GetServiceName(request);
            Context.Trace.WriteLine($"Looking for existing credential in store with service={service} account={request.UserName}...");

            ICredential credential = Context.CredentialStore.Get(service, request.UserName);
            if (credential == null)
            {
                Context.Trace.WriteLine("No existing credentials found.");

                // No existing credential was found, create a new one
                Context.Trace.WriteLine("Creating new credential...");
                credential = await GenerateCredentialAsync(request);
                Context.Trace.WriteLine("Credential created.");
            }
            else
            {
                Context.Trace.WriteLine("Existing credential found.");
            }

            return new GetCredentialResult(credential);
        }

        public virtual Task StoreCredentialAsync(GitRequest request)
        {
            string service = GetServiceName(request);

            // WIA-authentication is signaled to Git as an empty username/password pair
            // and we will get called to 'store' these WIA credentials.
            // We avoid storing empty credentials.
            if (string.IsNullOrWhiteSpace(request.UserName) && string.IsNullOrWhiteSpace(request.Password))
            {
                Context.Trace.WriteLine("Not storing empty credential.");
            }
            else
            {
                // Add or update the credential in the store.
                Context.Trace.WriteLine($"Storing credential with service={service} account={request.UserName}...");
                Context.CredentialStore.AddOrUpdate(service, request.UserName, request.Password);
                Context.Trace.WriteLine("Credential was successfully stored.");
            }

            return Task.CompletedTask;
        }

        public virtual Task EraseCredentialAsync(GitRequest request)
        {
            string service = GetServiceName(request);

            // Try to locate an existing credential
            Context.Trace.WriteLine($"Erasing stored credential in store with service={service} account={request.UserName}...");
            if (Context.CredentialStore.Remove(service, request.UserName))
            {
                Context.Trace.WriteLine("Credential was successfully erased.");
            }
            else
            {
                Context.Trace.WriteLine("No credential was erased.");
            }

            return Task.CompletedTask;
        }
    }
}
