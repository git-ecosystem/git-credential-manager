using System;
using System.Collections.Generic;
using System.IO;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents a file system and operations that can be performed.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Get the path to the user's home profile directory (Unix: $HOME, Windows: %USERPROFILE%).
        /// </summary>
        string UserHomePath { get; }

        /// <summary>
        /// Get the path the the user's Git Credential Manager data directory.
        /// </summary>
        string UserDataDirectoryPath { get; }

        /// <summary>
        /// Check if two paths are the same for the current platform and file system. Symbolic links are not followed.
        /// </summary>
        /// <param name="a">File path.</param>
        /// <param name="b">File path.</param>
        /// <returns>True if both file paths are the same, false otherwise.</returns>
        bool IsSamePath(string a, string b);

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

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">Name of the file to be deleted.</param>
        void DeleteFile(string path);

        /// <summary>
        /// Returns an enumerable collection of full file names that match a search pattern in a specified path.
        /// </summary>
        /// <param name="path">The relative or absolute path to the directory to search.</param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path.
        /// This parameter can contain a combination of valid literal path and wildcard (* and ?) characters,
        /// but it doesn't support regular expressions.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the files in the directory
        /// specified by path and that match the specified search pattern.
        /// </returns>
        IEnumerable<string> EnumerateFiles(string path, string searchPattern);

        /// <summary>
        /// Returns an enumerable collection of directory full names in a specified path.
        /// </summary>
        /// <param name="path">The relative or absolute path to the directory to search. This string is not case-sensitive.</param>
        /// <returns>
        /// An enumerable collection of the full names (including paths) for the directories
        /// in the directory specified by path.
        /// </returns>
        IEnumerable<string> EnumerateDirectories(string path);

        /// <summary>
        /// Opens a text file, reads all the text in the file, and then closes the file
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A string containing all the text in the file.</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A string array containing all lines of the file.</returns>
        string[] ReadAllLines(string path);
    }

    /// <summary>
    /// The real file system.
    /// </summary>
    public abstract class FileSystem : IFileSystem
    {
        public string UserHomePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public string UserDataDirectoryPath => Path.Combine(UserHomePath, Constants.GcmDataDirectoryName);

        public abstract bool IsSamePath(string a, string b);

        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

        public Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
            => File.Open(path, fileMode, fileAccess, fileShare);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteFile(string path) => File.Delete(path);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern) => Directory.EnumerateFiles(path, searchPattern);

        public IEnumerable<string> EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public string[] ReadAllLines(string path) => File.ReadAllLines(path);
    }
}
