using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GitCredentialManager.Tests.Objects
{
    public class TestFileSystem : IFileSystem
    {
        public string UserHomePath { get; set; }
        public string UserDataDirectoryPath { get; set; }
        public IDictionary<string, byte[]> Files { get; set; } = new Dictionary<string, byte[]>();
        public ISet<string> Directories { get; set; } = new HashSet<string>();
        public string CurrentDirectory { get; set; } = Path.GetTempPath();
        public bool IsCaseSensitive { get; set; } = false;

        public TestFileSystem()
        {
            var gcmTestRoot = Path.Combine(Path.GetTempPath(), $"gcmtest-{Guid.NewGuid():N}");
            UserHomePath = Path.Combine(gcmTestRoot, "HOME");
            UserDataDirectoryPath = Path.Combine(UserHomePath, ".gcm");
        }

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
            return Directories.Contains(TrimSlash(path));
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
            Directories.Add(TrimSlash(path));
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

        IEnumerable<string> IFileSystem.EnumerateDirectories(string path)
        {
            StringComparison comparer = IsCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            foreach (var dirPath in Directories)
            {
                if (dirPath.StartsWith(path, comparer))
                {
                    yield return dirPath;
                }
            }
        }

        string IFileSystem.ReadAllText(string path)
        {
            if (Files.TryGetValue(path, out byte[] data))
            {
                return Encoding.UTF8.GetString(data);
            }

            throw new IOException("File not found");
        }

        string[] IFileSystem.ReadAllLines(string path)
        {
            if (Files.TryGetValue(path, out byte[] data))
            {
                return Encoding.UTF8.GetString(data).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            }

            throw new IOException("File not found");
        }

        #endregion

        /// <summary>
        /// Trim trailing slashes from a path.
        /// </summary>
        public static string TrimSlash(string path)
        {
            if (path.Length > 0 && path[path.Length - 1] == Path.DirectorySeparatorChar)
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }
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
