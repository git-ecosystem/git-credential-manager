// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;

namespace Microsoft.Git.CredentialManager.Interop.MacOS
{
    public class MacOSFileSystem : FileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            a = Path.GetFileName(a);
            b = Path.GetFileName(b);

            // TODO: resolve symlinks
            // TODO: check if APFS/HFS+ is in case-sensitive mode
            return StringComparer.OrdinalIgnoreCase.Equals(a, b);
        }
    }
}
