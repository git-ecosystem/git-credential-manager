// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager.Interop.MacOS
{
    public class MacOSSystemPrompts : ISystemPrompts
    {
        public object ParentWindowId { get; set; }

        public bool ShowCredentialPrompt(string resource, string userName, out ICredential credential)
        {
            throw new System.NotImplementedException();
        }
    }
}
