// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestFileSystem : IFileSystem
    {
        public IDictionary<string, byte[]> Files { get; set; } = new Dictionary<string, byte[]>();
        public ISet<string> Directories { get; set; } = new HashSet<string>();
        public string CurrentDirectory { get; set; } = Path.GetTempPath();
        public bool IsCaseSensitive { get; set; } = false;

        #region IFileSystem

        bool IFileSystem.IsSamePath(string a, string b)
        {
            return IsCaseSensitive
                ? StringComparer.Ordinal.Equals(a, b)
                : StringComparer.OrdinalIgnoreCase.Equals(a, b);
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
            bool writable = fileAccess != FileAccess.Read;

            if (fileMode == FileMode.Create)
            {
                return new TestFileStream(this, path);
            }

            return new MemoryStream(Files[path], writable);
        }

        void IFileSystem.CreateDirectory(string path)
        {
            Directories.Add(path);
        }

        void IFileSystem.DeleteFile(string path)
        {
            Files.Remove(path);
        }

        IEnumerable<string> IFileSystem.EnumerateFiles(string path, string searchPattern)
        {
            bool IsPatternMatch(string s, string p)
            {
                var options = IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                string regex = p
                    .Replace(".", "\\.")
                    .Replace("*", ".*");

                return Regex.IsMatch(s, regex, options);
            }

            StringComparison comparer = IsCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            foreach (var filePath in Files.Keys)
            {
                if (filePath.StartsWith(path, comparer) && IsPatternMatch(filePath, searchPattern))
                {
                    yield return filePath;
                }
            }
        }

        #endregion
    }

    public class TestFileStream : MemoryStream
    {
        private readonly TestFileSystem _fs;
        private readonly string _path;

        public TestFileStream(TestFileSystem fs, string path)
        {
            _fs = fs;
            _path = path;
        }

        public override void Flush()
        {
            base.Flush();
            _fs.Files[_path] = base.ToArray();
        }
    }
}
