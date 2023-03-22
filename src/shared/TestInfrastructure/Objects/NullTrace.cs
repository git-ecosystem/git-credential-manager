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

        public void Initialize(DateTimeOffset startTime) { }

        public void Start(string appPath,
            string[] args,
            string filePath = "",
            int lineNumber = 0) { }

        public void Stop(int exitCode,
            string fileName,
            int lineNumber) { }

        public void WriteChildStart(DateTimeOffset startTime,
            Trace2ProcessClass processClass,
            bool useShell,
            string appName,
            string argv,
            string filePath = "",
            int lineNumber = 0) { }

        public void WriteChildExit(
            double relativeTime,
            int pid,
            int code,
            string filePath = "",
            int lineNumber = 0) { }

        public void WriteError(
            string errorMessage,
            string parameterizedMessage = null,
            string filePath = "",
            int lineNumber = 0) { }

        public Region CreateRegion(
            string category,
            string label,
            string message = "",
            string filePath = "",
            int lineNumber = 0)
        {
            return new Region(this, category, label, filePath, lineNumber, message);
        }

        public void WriteRegionEnter(
            string category,
            string label,
            string message = "",
            string filePath = "",
            int lineNumber = 0) { }

        public void WriteRegionLeave(
            double relativeTime,
            string category,
            string label,
            string message = "",
            string filePath = "",
            int lineNumber = 0) { }

        #endregion

        #region IDisposable

        void IDisposable.Dispose() { }

        #endregion
    }
}
