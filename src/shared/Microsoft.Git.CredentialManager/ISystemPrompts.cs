// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Git.CredentialManager
{
    public interface ISystemPrompts
    {
        bool ShowCredentialPrompt(string resource, string userName, out ICredential credential);
    }
}
