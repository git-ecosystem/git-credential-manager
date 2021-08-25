using System;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestSystemPrompts : ISystemPrompts
    {
        public Func<string, string, ICredential> CredentialPrompt { get; set; } = (resource, user) => null;

        public object ParentWindowId { get; set; }

        public bool ShowCredentialPrompt(string resource, string userName, out ICredential credential)
        {
            credential = CredentialPrompt(resource, userName);
            return credential != null;
        }
    }
}
