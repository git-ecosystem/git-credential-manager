// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.Git.CredentialManager.Interop.MacOS;
using Microsoft.Git.CredentialManager.Interop.Posix;
using Microsoft.Git.CredentialManager.Interop.Windows;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents the execution environment for a Git credential helper command.
    /// </summary>
    public interface ICommandContext
    {
        /// <summary>
        /// The standard input text stream from the calling process, typically Git.
        /// </summary>
        TextReader StdIn { get; }

        /// <summary>
        /// The standard output text stream connected back to the calling process, typically Git.
        /// </summary>
        TextWriter StdOut { get; }

        /// <summary>
        /// The standard error text stream connected back to the calling process, typically Git.
        /// </summary>
        TextWriter StdError { get; }

        /// <summary>
        /// The attached terminal (TTY) to this process tree.
        /// </summary>
        ITerminal Terminal { get; }

        /// <summary>
        /// Application tracing system.
        /// </summary>
        ITrace Trace { get; }

        /// <summary>
        /// File system abstraction (exists mainly for testing).
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Secure credential storage.
        /// </summary>
        ICredentialStore CredentialStore { get; }

        /// <summary>
        /// Access the environment variables for the current GCM process.
        /// </summary>
        /// <returns>Set of all current environment variables.</returns>
        IReadOnlyDictionary<string, string> EnvironmentVariables { get; }
    }

    /// <summary>
    /// Real command execution environment using the actual <see cref="Console"/>, file system calls and environment.
    /// </summary>
    public class CommandContext : ICommandContext
    {
        private const string LineFeed  = "\n";

        private static readonly Encoding Utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private TextReader _stdIn;
        private TextWriter _stdOut;
        private TextWriter _stdErr;
        private ITerminal _terminal;
        private IReadOnlyDictionary<string, string> _envars;

        #region ICommandContext

        public TextReader StdIn
        {
            get
            {
                if (_stdIn == null)
                {
                    _stdIn = new StreamReader(Console.OpenStandardInput(), Utf8NoBomEncoding);
                }

                return _stdIn;
            }
        }

        public TextWriter StdOut
        {
            get
            {
                if (_stdOut == null)
                {
                    _stdOut = new StreamWriter(Console.OpenStandardOutput(), Utf8NoBomEncoding)
                    {
                        AutoFlush = true,
                        NewLine = LineFeed,
                    };
                }

                return _stdOut;
            }
        }

        public TextWriter StdError
        {
            get
            {
                if (_stdErr == null)
                {
                    _stdErr = new StreamWriter(Console.OpenStandardError(), Utf8NoBomEncoding)
                    {
                        AutoFlush = true,
                        NewLine = LineFeed,
                    };
                }

                return _stdErr;
            }
        }

        public ITerminal Terminal
        {
            get { return _terminal ?? (_terminal = CreateTerminal()); }
        }

        public ITrace Trace { get; } = new Trace();

        public IFileSystem FileSystem { get; } = new FileSystem();

        public ICredentialStore CredentialStore { get; } = CreateCredentialStore();

        public IReadOnlyDictionary<string, string> EnvironmentVariables
        {
            get
            {
                if (_envars is null)
                {
                    IDictionary variables = Environment.GetEnvironmentVariables();

                    // On Windows it is technically possible to get env vars which differ only by case
                    // even though the general assumption is that they are case insensitive on Windows.
                    // For example, some of the standard .NET types like System.Diagnostics.Process
                    // will fail to start a process on Windows if given duplicate environment variables.
                    // See this issue for more information: https://github.com/dotnet/corefx/issues/13146

                    // If we're on the Windows platform we should de-duplicate by setting the string
                    // comparer to OrdinalIgnoreCase.
                    var comparer = PlatformUtils.IsWindows()
                        ? StringComparer.OrdinalIgnoreCase
                        : StringComparer.Ordinal;

                    var dict = new Dictionary<string, string>(comparer);

                    foreach (var key in variables.Keys)
                    {
                        if (key is string name && variables[key] is string value)
                        {
                            dict[name] = value;
                        }
                    }

                    _envars = new ReadOnlyDictionary<string, string>(dict);
                }

                return _envars;
            }
        }

        #endregion

        private ITerminal CreateTerminal()
        {
            if (PlatformUtils.IsPosix())
            {
                return new PosixTerminal(Trace);
            }

            if (PlatformUtils.IsWindows())
            {
                return new WindowsTerminal(Trace);
            }

            throw new PlatformNotSupportedException();
        }

        private static ICredentialStore CreateCredentialStore()
        {
            if (PlatformUtils.IsMacOS())
            {
                return MacOSKeychain.Open();
            }

            if (PlatformUtils.IsWindows())
            {
                return WindowsCredentialManager.Open();
            }

            if (PlatformUtils.IsLinux())
            {
                throw new NotImplementedException();
            }

            throw new PlatformNotSupportedException();
        }
    }

    public static class CommandContextExtensions
    {
        /// <summary>
        /// Try to get the current value of the specified environment variable.
        /// </summary>
        /// <param name="context"><see cref="ICommandContext"/></param>
        /// <param name="key">The name of environment variable.</param>
        /// <param name="value">The current value of the environment variable.</param>
        /// <returns>True if the environment variable was set and has a value, false otherwise.</returns>
        public static bool TryGetEnvironmentVariable(this ICommandContext context, string key, out string value)
        {
            return context.EnvironmentVariables.TryGetValue(key, out value);
        }

        /// <summary>
        /// Test if the specified environment variable is 'truthy' (considered to be equivalent to a 'true' value
        /// by <see cref="StringExtensions.IsTruthy"/>).
        /// </summary>
        /// <param name="context"><see cref="ICommandContext"/></param>
        /// <param name="key">The name of environment variable.</param>
        /// <param name="defaultValue">
        /// The assumed default value of the environment variable if it does not
        /// exist/has not been set.
        /// </param>
        /// <returns>True if the environment variable was set and has a 'truthy' value, <see cref="defaultValue"/> otherwise.</returns>
        public static bool IsEnvironmentVariableTruthy(this ICommandContext context, string key, bool defaultValue)
        {
            if (context.TryGetEnvironmentVariable(key, out string valueStr) && valueStr.IsTruthy())
            {
                return true;
            }

            return defaultValue;
        }
    }
}
