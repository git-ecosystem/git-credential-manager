using System;
using System.Diagnostics;

namespace GitCredentialManager
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
        private readonly IProcessManager _processManager;
        private readonly ITrace2 _trace2;

        public Gpg(string gpgPath, ISessionManager sessionManager, IProcessManager processManager, ITrace2 trace2)
        {
            EnsureArgument.NotNullOrWhiteSpace(gpgPath, nameof(gpgPath));
            EnsureArgument.NotNull(sessionManager, nameof(sessionManager));
            EnsureArgument.NotNull(trace2, nameof(trace2));

            _gpgPath = gpgPath;
            _sessionManager = sessionManager;
            _processManager = processManager;
            _trace2 = trace2;
        }

        public string DecryptFile(string path)
        {
            var psi = new ProcessStartInfo(_gpgPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                // Suppress verbose decryption messages
                // Ok to redirect stderr for non-Git-related processes
                RedirectStandardError = true,
            };
#if NETFRAMEWORK
            psi.Arguments = $"--batch --decrypt \"{path}\"";
#else
            psi.ArgumentList.Add("--batch");
            psi.ArgumentList.Add("--decrypt");
            psi.ArgumentList.Add(path);
#endif

            PrepareEnvironment(psi);

            using (var gpg = _processManager.CreateProcess(psi))
            {
                if (!gpg.Start(Trace2ProcessClass.Other))
                {
                    throw new Trace2Exception(_trace2, "Failed to start gpg.");
                }

                gpg.WaitForExit();

                if (gpg.ExitCode != 0)
                {
                    string stdout = gpg.StandardOutput.ReadToEnd();
                    string stderr = gpg.StandardError.ReadToEnd();
                    var format = "Failed to decrypt file '{0}' with gpg. exit={1}, out={2}, err={3}";
                    var message = string.Format(format, path, gpg.ExitCode, stdout, stderr);
                    throw new Trace2Exception(_trace2, message, format);
                }

                return gpg.StandardOutput.ReadToEnd();
            }
        }

        public void EncryptFile(string path, string gpgId, string contents)
        {
            var psi = new ProcessStartInfo(_gpgPath)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Ok to redirect stderr for non-git-related processes
            };
#if NETFRAMEWORK
            psi.Arguments = $"--encrypt --batch --recipient \"{gpgId}\" --output \"{path}\"";
#else
            psi.ArgumentList.Add("--encrypt");
            psi.ArgumentList.Add("--batch");
            psi.ArgumentList.Add("--recipient");
            psi.ArgumentList.Add(gpgId);
            psi.ArgumentList.Add("--output");
            psi.ArgumentList.Add(path);
#endif

            PrepareEnvironment(psi);

            using (var gpg = _processManager.CreateProcess(psi))
            {
                if (!gpg.Start(Trace2ProcessClass.Other))
                {
                    throw new Trace2Exception(_trace2, "Failed to start gpg.");
                }

                gpg.StandardInput.Write(contents);
                gpg.StandardInput.Close();

                gpg.WaitForExit();

                if (gpg.ExitCode != 0)
                {
                    string stdout = gpg.StandardOutput.ReadToEnd();
                    string stderr = gpg.StandardError.ReadToEnd();
                    var format = "Failed to encrypt file '{0}' with gpg. exit={1}, out={2}, err={3}";
                    var message = string.Format(format, path, gpg.ExitCode, stdout, stderr);
                    throw new Trace2Exception(_trace2, message, format);
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
