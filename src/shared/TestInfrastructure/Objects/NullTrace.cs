using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitCredentialManager.Tests.Objects
{
    public class NullTrace : ITrace
    {
        #region ITrace

        bool ITrace.HasListeners
        {
            get => false;
        }

        bool ITrace.IsSecretTracingEnabled
        {
            get => false;
            set {}
        }

        void ITrace.AddListener(TextWriter listener) { }

        void ITrace.Flush() { }

        void ITrace.WriteException(Exception exception, string filePath, int lineNumber, string memberName) { }

        void ITrace.WriteDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary, string filePath, int lineNumber, string memberName) { }

        public void WriteDictionarySecrets<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey[] secretKeys,
            IEqualityComparer<TKey> keyComparer = null, string filePath = "", int lineNumber = 0,
            string memberName = "") { }

        void ITrace.WriteLine(string message, string filePath, int lineNumber, string memberName) { }

        void ITrace.WriteLineSecrets(
            string format, object[] secrets, string filePath, int lineNumber, string memberName) { }

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }

    public class NullTrace2 : ITrace2
    {
        #region ITrace2
        public void AddWriter(ITrace2Writer writer) { }

        public void Start(TextWriter error,
            IFileSystem fileSystem,
            string appPath,
            string filePath = "",
            int lineNumber = 0) { }

        public void Stop(int exitCode, string fileName, int lineNumber) { }

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }
}
