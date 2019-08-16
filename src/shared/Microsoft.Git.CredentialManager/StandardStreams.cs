// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager
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
    }

    public class StandardStreams : IStandardStreams
    {
        private const string LineFeed  = "\n";

        private static readonly Encoding Utf8NoBomEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private TextReader _stdIn;
        private TextWriter _stdOut;
        private TextWriter _stdErr;

        public TextReader In
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

        public TextWriter Out
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

        public TextWriter Error
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
    }
}
