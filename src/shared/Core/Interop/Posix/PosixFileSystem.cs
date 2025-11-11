using System;
using System.IO;

namespace GitCredentialManager.Interop.Posix
{
    public abstract class PosixFileSystem : FileSystem
    {
        /// <summary>
        /// Recursively resolve a symbolic link.
        /// </summary>
        /// <param name="path">Path to resolve.</param>
        /// <returns>Resolved symlink, or original path if not a link.</returns>
        /// <exception cref="ArgumentException">Path is not absolute.</exception>
        protected internal static string ResolveSymbolicLinks(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentException("Path must be absolute", nameof(path));
            }

            // If the file or directory doesn't actually exist we cannot resolve
            // any symlinks, so just return the original input path.
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return path;
            }

            // If the file is a symlink then resolve it!
            string realPath = TryResolveFileLink(path, out string resolvedFile)
                ? resolvedFile
                : path;

            // Work backwards from the file name resolving directories symlinks
            string partialPath = Path.GetFileName(realPath);
            string dirPath = Path.GetDirectoryName(realPath);
            while (dirPath != null)
            {
                // Try to resolve directory symlinks
                if (TryResolveDirectoryLink(dirPath, out string resolvedDir))
                {
                    dirPath = resolvedDir;
                }

                string dirName = Path.GetFileName(dirPath);
                partialPath = Path.Combine(dirName, partialPath);
                dirPath = Path.GetDirectoryName(dirPath);
            }

            return Path.Combine("/", partialPath);
        }

        private static bool TryResolveFileLink(string path, out string target)
        {
            FileSystemInfo fsi = File.ResolveLinkTarget(path, true);
            target = fsi?.FullName;
            return fsi != null;
        }

        private static bool TryResolveDirectoryLink(string path, out string target)
        {
            FileSystemInfo fsi = Directory.ResolveLinkTarget(path, true);
            target = fsi?.FullName;
            return fsi != null;
        }
    }
}
