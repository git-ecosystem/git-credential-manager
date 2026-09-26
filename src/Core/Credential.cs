
namespace GitCredentialManager
{
    /// <summary>
    /// Represents a credential.
    /// </summary>
    public interface ICredential
    {
        /// <summary>
        /// Account associated with this credential.
        /// </summary>
        string Account { get; }

        /// <summary>
        /// Password.
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Credential can have a limited lifetime.
        /// </summary>
        bool IsEphemeral => false;
    }

    /// <summary>
    /// Represents a credential (username/password pair) that Git can use to authenticate to a remote repository.
    /// </summary>
    public class GitCredential(string userName, string password, bool isEphemeral = false) : ICredential
    {
        public string Account { get; } = userName;

        public string Password { get; } = password;

        public bool IsEphemeral { get; } = isEphemeral;
    }
}
