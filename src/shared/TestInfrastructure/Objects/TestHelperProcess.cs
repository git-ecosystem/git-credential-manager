// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestHelperProcess : IHelperProcess
    {
        public Task<IDictionary<string, string>> InvokeAsync(string path, string args, IDictionary<string, string> standardInput)
        {
            throw new NotImplementedException();
        }
    }
}
