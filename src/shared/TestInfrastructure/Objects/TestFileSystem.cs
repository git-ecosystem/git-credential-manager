// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestFileSystem : IFileSystem
    {
        public IDictionary<string, MemoryStream> Files { get; set; } = new Dictionary<string, MemoryStream>();
        public ISet<string> Directories { get; set; } = new HashSet<string>();
        public string CurrentDirectory { get; set; }
        public string UserHomePath { get; set; }
        public string UserDataDirectoryPath { get; set; }

        public TestFileSystem()
        {
            var gcmTestRoot = Path.Combine(Path.GetTempPath(), $"gcmtest-{Guid.NewGuid():N}");
            UserHomePath = Path.Combine(gcmTestRoot, "HOME");
            UserDataDirectoryPath = Path.Combine(UserHomePath, ".gcm");
        }

        #region IFileSystem

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
            MemoryStream stream;

            bool writable = fileAccess == FileAccess.Write || fileAccess == FileAccess.ReadWrite;

            // Simulate System.IO.FileStream
            switch (fileMode)
            {
                case FileMode.Append:
                    if (!writable) throw new IOException();
                    stream = Files[path];
                    stream.Seek(0, SeekOrigin.End);
                    break;

                case FileMode.Create:
                    Files[path] = new MemoryStream();
                    stream = Files[path];
                    break;

                case FileMode.CreateNew:
                    if (Files.ContainsKey(path)) throw new IOException();
                    Files[path] = new MemoryStream();
                    stream = Files[path];
                    break;

                case FileMode.Open:
                    if (!Files.ContainsKey(path)) throw new FileNotFoundException();
                    stream = Files[path];
                    stream.Seek(0, SeekOrigin.Begin);
                    break;

                case FileMode.OpenOrCreate:
                    if (!Files.TryGetValue(path, out stream))
                    {
                        Files[path] = new MemoryStream();
                        stream = Files[path];
                    }
                    stream.Seek(0, SeekOrigin.Begin);
                    break;

                case FileMode.Truncate:
                    Files[path] = new MemoryStream();
                    stream = Files[path];
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(fileMode), fileMode, "Unknown FileMode");
            }

            return stream;
        }

        public string ReadAllText(string path)
        {
            var bytes = Files[path].ToArray();
            return Encoding.UTF8.GetString(bytes);
        }

        public void WriteAllText(string path, string contents)
        {
            var bytes = Encoding.UTF8.GetBytes(contents);
            Files[path] = new MemoryStream();
            Files[path].Write(bytes, 0, bytes.Length);
        }

        void IFileSystem.CreateDirectory(string path)
        {
            Directories.Add(path);
        }

        void IFileSystem.DeleteFile(string path)
        {
            Files.Remove(path);
        }

        #endregion
    }
}
