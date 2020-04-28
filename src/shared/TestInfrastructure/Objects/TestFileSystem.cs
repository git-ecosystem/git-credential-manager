// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestFileSystem : IFileSystem
    {
        public IDictionary<string, Stream> Files { get; set; } = new Dictionary<string, Stream>();
        public ISet<string> Directories { get; set; } = new HashSet<string>();
        public string CurrentDirectory { get; set; } = Path.GetTempPath();
        public IEqualityComparer<string> PathComparer { get; set; }= StringComparer.OrdinalIgnoreCase;

        #region IFileSystem

        bool IFileSystem.IsSamePath(string a, string b)
        {
            return PathComparer.Equals(a, b);
        }

        bool IFileSystem.FileExists(string path)
        {
            return Files.ContainsKey(path);
        }

        bool IFileSystem.DirectoryExists(string path)
        {
            return Directories.Contains(path);
        }

        string IFileSystem.GetCurrentDirectory()
        {
            return CurrentDirectory;
        }

        Stream IFileSystem.OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return Files[path];
        }

        #endregion
    }
}
