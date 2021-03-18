// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a file system and operations that can be performed.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Get the path to the user's home profile directory ($HOME, %USERPROFILE%).
        /// </summary>
        string UserHomePath { get; }

        /// <summary>
        /// Get the path the the user's Git Credential Manager data directory.
        /// </summary>
        string UserDataDirectoryPath { get; }

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
        /// Opens a text file, reads all lines of the file, and then closes the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>A string containing all lines of the file.</returns>
        string ReadAllText(string path);

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes the file.
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file</param>
        void WriteAllText(string path, string contents);

        /// <summary>
        /// Creates directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The file to delete.</param>
        void DeleteFile(string path);
    }

    /// <summary>
    /// The real file system.
    /// </summary>
    public class FileSystem : IFileSystem
    {
        public string UserHomePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public string UserDataDirectoryPath => Path.Combine(UserHomePath, ".gcm");

        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

        public Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
            => File.Open(path, fileMode, fileAccess, fileShare);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteFile(string path) => File.Delete(path);
    }
}
