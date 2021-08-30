using System;
using System.Diagnostics;

namespace Microsoft.Git.CredentialManager
{
    public interface IGpg
    {
        string DecryptFile(string path);

        void EncryptFile(string path, string gpgId, string contents);
    }

    public class Gpg : IGpg
    {
        private readonly string _gpgPath;
        private readonly ISessionManager _sessionManager;

        public Gpg(string gpgPath, ISessionManager sessionManager)
        {
            EnsureArgument.NotNullOrWhiteSpace(gpgPath, nameof(gpgPath));
            EnsureArgument.NotNull(sessionManager, nameof(sessionManager));

            _gpgPath = gpgPath;
            _sessionManager = sessionManager;
        }

        public string DecryptFile(string path)
        {
            var psi = new ProcessStartInfo(_gpgPath, $"--batch --decrypt \"{path}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Suppress verbose decryption messages
            };

            PrepareEnvironment(psi);

            using (var gpg = Process.Start(psi))
            {
                if (gpg is null)
                {
                    throw new Exception("Failed to start gpg.");
                }

                gpg.WaitForExit();

                if (gpg.ExitCode != 0)
                {
                    string stdout = gpg.StandardOutput.ReadToEnd();
                    string stderr = gpg.StandardError.ReadToEnd();
                    throw new Exception($"Failed to decrypt file '{path}' with gpg. exit={gpg.ExitCode}, out={stdout}, err={stderr}");
                }

                return gpg.StandardOutput.ReadToEnd();
            }
        }

        public void EncryptFile(string path, string gpgId, string contents)
        {
            var psi = new ProcessStartInfo(_gpgPath, $"--encrypt --batch --recipient \"{gpgId}\" --output \"{path}\"")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            PrepareEnvironment(psi);

            using (var gpg = Process.Start(psi))
            {
                if (gpg is null)
                {
                    throw new Exception("Failed to start gpg.");
                }

                gpg.StandardInput.Write(contents);
                gpg.StandardInput.Close();

                gpg.WaitForExit();

                if (gpg.ExitCode != 0)
                {
                    string stdout = gpg.StandardOutput.ReadToEnd();
                    string stderr = gpg.StandardError.ReadToEnd();
                    throw new Exception($"Failed to encrypt file '{path}' with gpg. exit={gpg.ExitCode}, out={stdout}, err={stderr}");
                }
            }
        }

        private void PrepareEnvironment(ProcessStartInfo psi)
        {
            // If we're in a headless environment over SSH, and we don't have a GPG_TTY
            // explicitly set, use the SSH_TTY variable for our GPG_TTY.
            if (!_sessionManager.IsDesktopSession &&
                !psi.Environment.ContainsKey("GPG_TTY") &&
                psi.Environment.ContainsKey("SSH_TTY"))
            {
                psi.Environment["GPG_TTY"] = psi.Environment["SSH_TTY"];
            }
        }
    }
}
