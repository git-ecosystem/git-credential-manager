using System;
using System.IO;

namespace GitCredentialManager
{
    /// <summary>
    /// Represents the standard I/O streams (input, output, error) of a process.
    /// </summary>
    public interface IStandardStreams
    {
        /// <summary>
        /// The standard input text stream from the calling process, typically Git.
        /// </summary>
        TextReader In { get; }

        /// <summary>
        /// The standard output text stream connected back to the calling process, typically Git.
        /// </summary>
        TextWriter Out { get; }

        /// <summary>
        /// The standard error text stream connected back to the calling process, typically Git.
        /// </summary>
        TextWriter Error { get; }

        /// <summary>
        /// Returns true if the <see cref="In"/> stream is redirected, and false otherwise.
        /// </summary>
        bool IsInputRedirected { get; }

        /// <summary>
        /// Returns true if the <see cref="Out"/> stream is redirected, and false otherwise.
        /// </summary>
        bool IsOutputRedirected { get; }

        /// <summary>
        /// Returns true if the <see cref="Error"/> stream is redirected, and false otherwise.
        /// </summary>
        bool IsErrorRedirected { get; }
    }

    public class StandardStreams : IStandardStreams
    {
        private const string LineFeed  = "\n";

        private TextReader _stdIn;
        private TextWriter _stdOut;
        private TextWriter _stdErr;

        public TextReader In
        {
            get
            {
                if (_stdIn == null)
                {
                    _stdIn = new GitStreamReader(Console.OpenStandardInput(), EncodingEx.UTF8NoBom);
                }

                return _stdIn;
            }
        }

        public TextWriter Out
        {
            get
            {
                if (_stdOut == null)
                {
                    _stdOut = new StreamWriter(Console.OpenStandardOutput(), EncodingEx.UTF8NoBom)
                    {
                        AutoFlush = true,
                        NewLine = LineFeed,
                    };
                }

                return _stdOut;
            }
        }

        public TextWriter Error
        {
            get
            {
                if (_stdErr == null)
                {
                    _stdErr = new StreamWriter(Console.OpenStandardError(), EncodingEx.UTF8NoBom)
                    {
                        AutoFlush = true,
                        NewLine = LineFeed,
                    };
                }

                return _stdErr;
            }
        }

        public bool IsInputRedirected => Console.IsInputRedirected;
        public bool IsOutputRedirected => Console.IsOutputRedirected;
        public bool IsErrorRedirected => Console.IsErrorRedirected;
    }
}
