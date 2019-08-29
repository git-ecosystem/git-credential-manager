// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using Microsoft.Git.CredentialManager.Interop.Native;
using static Microsoft.Git.CredentialManager.Interop.Native.git_config_level_t;
using static Microsoft.Git.CredentialManager.Interop.Native.LibGit2;

namespace Microsoft.Git.CredentialManager.Interop
{
    public class LibGit2 : IGit
    {
        private readonly ITrace _trace;

        public LibGit2(ITrace trace)
        {
            EnsureArgument.NotNull(trace, nameof(trace));

            _trace = trace;

            _trace.WriteLine("Initializing libgit2...");
            git_libgit2_init();
        }

        public unsafe IGitConfiguration GetConfiguration(string repositoryPath)
        {
            _trace.WriteLine("Opening default Git configuration...");

            // Open the default, non-repository-scoped configuration (progdata, system, xdg, global)
            // Note that currently git_config_open_default(git_config** does not search /usr/local/etc for Git configuration
            // files (such as those used by Homebrew installations on macOS, or locally built Git instances).
            // We don't workaround this and push the responsibility to correct this with libgit2.
            git_config* config;
            ThrowIfError(git_config_open_default(&config), nameof(git_config_open_default));

            // If we have a repository path then also include the local repository configuration
            if (repositoryPath != null)
            {
                // We don't need to check for the file's existence since libgit2 will do that for us!
                string repoConfigPath = Path.Combine(repositoryPath, ".git", "config");

                // Add the repository configuration
                _trace.WriteLine($"Adding local configuration from repository '{repositoryPath}'...");
                int error = git_config_add_file_ondisk(config, repoConfigPath, GIT_CONFIG_LEVEL_LOCAL, null, 0);
                switch (error)
                {
                    case GIT_OK:
                    case GIT_ENOTFOUND: // If the file was not found we should just continue
                        break;
                    default:
                        git_config_free(config);
                        ThrowIfError(error, nameof(git_config_add_file_ondisk));
                        return null;
                }
            }

            return new LibGit2Configuration(_trace, config);
        }

        public string GetRepositoryPath(string path)
        {
            _trace.WriteLine($"Discovering repository from path '{path}'...");

            var buf = new git_buf();
            int error = git_repository_discover(buf, path, true, null);

            try
            {
                switch (error)
                {
                    case GIT_OK:
                        string repoPath = buf.ToString();
                        _trace.WriteLine($"Found repository at '{repoPath}'.");
                        return repoPath;
                    case GIT_ENOTFOUND:
                        return null;
                    default:
                        ThrowIfError(error, nameof(git_repository_discover));
                        return null;
                }
            }
            finally
            {
                git_buf_dispose(buf);
            }
        }

        private void Dispose(bool disposing)
        {
            git_libgit2_shutdown();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LibGit2()
        {
            Dispose(false);
        }
    }

    internal class LibGit2Configuration : IGitConfiguration
    {
        private readonly ITrace _trace;
        private readonly unsafe git_config* _config;
        private readonly unsafe git_config* _snapshot;

        internal unsafe LibGit2Configuration(ITrace trace, git_config* config)
        {
            _trace = trace;
            _config = config;

            // Create snapshot for reading values
            _trace.WriteLine("Creating Git configuration snapshot...");
            git_config* snapshot = null;
            ThrowIfError(git_config_snapshot(&snapshot, config), nameof(git_config_snapshot));
            _snapshot = snapshot;
        }

        public unsafe void Enumerate(GitConfigurationEnumerationCallback cb)
        {
            int native_cb(git_config_entry entry, void* payload)
            {
                if (!cb(entry.GetName(), entry.GetValue()))
                {
                    return GIT_ITEROVER;
                }

                return GIT_OK;
            }

            _trace.WriteLine("Enumerating Git configuration entries...");
            var result = git_config_foreach(_config, native_cb, (void*) IntPtr.Zero);

            switch (result)
            {
                case GIT_OK:
                case GIT_ITEROVER:
                    _trace.WriteLine("Enumeration complete.");
                    break;
                default:
                    ThrowIfError(result, nameof(git_config_foreach));
                    break;
            }
        }

        public unsafe bool TryGetValue(string name, out string value)
        {
            _trace.WriteLine($"Reading Git configuration entry '{name}'...");
            int result = git_config_get_string(out value, _snapshot, name);

            switch (result)
            {
                case GIT_OK:
                    _trace.WriteLine($"Successfully read value '{value}'.");
                    return true;
                case GIT_ENOTFOUND:
                    _trace.WriteLine("No entry found.");
                    value = null;
                    break;
                default:
                    ThrowIfError(result, nameof(git_config_get_string));
                    break;
            }

            return false;
        }

        private void Dispose(bool disposing)
        {
            unsafe
            {
                _trace.WriteLine("Disposing Git configuration...");
                git_config_free(_snapshot);
                git_config_free(_config);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LibGit2Configuration()
        {
            Dispose(false);
        }
    }
}
