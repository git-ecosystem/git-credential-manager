// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class NullTrace : ITrace
    {
        #region ITrace

        bool ITrace.EnableSecretTracing
        {
            get => false;
            set {}
        }

        void ITrace.AddListener(TextWriter listener) { }

        void ITrace.Flush() { }

        void ITrace.WriteException(Exception exception, string filePath, int lineNumber, string memberName) { }

        void ITrace.WriteDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary, string filePath, int lineNumber, string memberName) { }

        void ITrace.WriteLine(string message, string filePath, int lineNumber, string memberName) { }

        void ITrace.WriteLineSecrets(
            string format, object[] secrets, string filePath, int lineNumber, string memberName) { }

        #endregion
    }
}
