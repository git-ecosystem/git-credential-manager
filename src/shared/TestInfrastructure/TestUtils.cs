using System;
using System.IO;

namespace GitCredentialManager.Tests
{
    public static class TestUtils
    {
        public static IDisposable ChangeDirectory(string path)
        {
            var cookie = new ChangeDirectoryCookie(Environment.CurrentDirectory);
            Environment.CurrentDirectory = path;
            return cookie;
        }

        private class ChangeDirectoryCookie : IDisposable
        {
            private readonly string _oldPath;
            public ChangeDirectoryCookie(string oldPath) => _oldPath = oldPath;
            public void Dispose() => Environment.CurrentDirectory = _oldPath;
        }

        private static string JoinPaths(string basePath, params string[] names)
        {
            string path = basePath;
            foreach (string name in names)
            {
                path = Path.Combine(path, name);
            }
            return path;
        }

        public static string CreateFileSymlink(string baseDir, string linkName, string targetPath)
        {
            string linkPath = Path.Combine(baseDir, linkName);
            FileSystemInfo fsi = File.CreateSymbolicLink(linkPath, targetPath);
            return fsi.FullName;
        }

        public static string CreateDirectorySymlink(string baseDir, string linkName, string targetPath)
        {
            string linkPath = Path.Combine(baseDir, linkName);
            FileSystemInfo fsi = Directory.CreateSymbolicLink(linkPath, targetPath);
            return fsi.FullName;
        }

        public static string CreateFile(string baseDir, params string[] names)
        {
            string path = JoinPaths(baseDir, names);
            string parentDir = Path.GetDirectoryName(path);
            if (parentDir != null) Directory.CreateDirectory(parentDir);
            File.Create(path);
            return path;
        }

        public static string CreateDirectory(string baseDir, params string[] names)
        {
            string path = JoinPaths(baseDir, names);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string GetTempDirectory(bool create = true)
        {
            // Note that on macOS, Path.GetTempPath() returns a path
            // under /var that is actually a directory symlink to /private/var.
            // We should return a manually resolved path instead to ensure
            // callers can do path comparisons and computations that may resolve
            // symlinks.
            string tempDir = PlatformUtils.IsMacOS()
                ? "/private/tmp"
                : Path.GetTempPath();

            string unique = GetUuid(8);
            string path = Path.Combine(tempDir, unique);
            if (create) Directory.CreateDirectory(path);
            return path;
        }

        public static string GetUuid(int length = -1)
        {
            string uuid = Guid.NewGuid().ToString("N");

            if (length <= 0 || length > uuid.Length)
            {
                return uuid;
            }

            return uuid.Substring(0, length);
        }
    }
}
