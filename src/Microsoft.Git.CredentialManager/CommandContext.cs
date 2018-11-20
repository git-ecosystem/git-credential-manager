using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Git.CredentialManager
{
    public interface ICommandContext
    {
        TextReader StdIn { get; }

        TextWriter StdOut { get; }

        TextWriter StdError { get; }

        ITrace Trace { get; }

        IFileSystem FileSystem { get; }

        IReadOnlyDictionary<string, string> GetEnvironmentVariables();
    }

    public class CommandContext : ICommandContext
    {
        private const string LineFeed  = "\n";

        private static readonly Encoding Utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private TextReader _stdIn;
        private TextWriter _stdOut;
        private TextWriter _stdErr;

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

        public ITrace Trace { get; } = new Trace();

        public IFileSystem FileSystem { get; } = new FileSystem();

        public IReadOnlyDictionary<string, string> GetEnvironmentVariables()
        {
            IDictionary variables = Environment.GetEnvironmentVariables();

            // On Windows it is technically possible to get env vars which differ only by case
            // even though the assumption is that they are case insensitive on Windows.
            // If we're on the Windows platform we should de-duplicate by setting the string
            // comparer to OrdinalIgnoreCase.
            var comparer = PlatformUtils.IsWindows()
                         ? StringComparer.OrdinalIgnoreCase
                         : StringComparer.Ordinal;

            var result = new Dictionary<string, string>(comparer);

            foreach (var key in variables.Keys)
            {
                if (key is string name && variables[key] is string value)
                {
                    result[name] = value;
                }
            }

            return result;
        }

        #endregion
    }

    public static class CommandContextExtensions
    {
        public static bool TryGetEnvironmentVariable(this ICommandContext context, string key, out string value)
        {
            return context.GetEnvironmentVariables().TryGetValue(key, out value);
        }

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
