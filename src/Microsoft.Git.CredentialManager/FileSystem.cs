using System.IO;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a file system and operations that can be performed.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Check if a file exists at the specified path.
        /// </summary>
        /// <param name="path">Full path to file to test.</param>
        /// <returns>True if a file exists, false otherwise.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Check if a directory exists at the specified path.
        /// </summary>
        /// <param name="path">Full path to directory to test.</param>
        /// <returns>True if a directory exists, false otherwise.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Get the path to the current directory of the currently executing process.
        /// </summary>
        /// <returns>Current process directory.</returns>
        string GetCurrentDirectory();

        /// <summary>
        /// Open a file stream at the specified path with the given access and mode settings.
        /// </summary>
        /// <param name="path">Full file path.</param>
        /// <param name="fileMode">File mode settings.</param>
        /// <param name="fileAccess">File access settings.</param>
        /// <param name="fileShare">File share settings.</param>
        /// <returns></returns>
        Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);
    }

    /// <summary>
    /// The real file system.
    /// </summary>
    public class FileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

        public Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
            => File.Open(path, fileMode, fileAccess, fileShare);
    }
}
