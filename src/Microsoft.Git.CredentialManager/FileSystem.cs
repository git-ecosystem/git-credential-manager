using System.IO;

namespace Microsoft.Git.CredentialManager
{
    public interface IFileSystem
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        string GetCurrentDirectory();

        Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);
    }

    public class FileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

        public Stream OpenFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
            => File.Open(path, fileMode, fileAccess, fileShare);
    }
}
