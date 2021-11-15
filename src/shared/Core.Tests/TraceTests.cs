
using System;
using System.IO;
using System.Text;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class TraceTests
    {
        [Fact]
        public void Trace_WriteLineSecrets_SecretTracingEnabled_WritesSecretValues()
        {
            const string secret1 = "foo";
            const string secret2 = "bar";
            const string secret3 = "test";

            var sb = new StringBuilder();
            var listener = new StringWriter(sb);

            var trace = new Trace();
            trace.AddListener(listener);
            trace.IsSecretTracingEnabled = true;

            trace.WriteLineSecrets("Secrets: {0} {1} {2}", new object[]{ secret1, secret2, secret3 });

            string expectedTraceEnd = $"Secrets: {secret1} {secret2} {secret3}\n";
            string actualTrace = sb.ToString();

            Assert.EndsWith(expectedTraceEnd, actualTrace, StringComparison.Ordinal);
        }

        [Fact]
        public void Trace_WriteLineSecrets_SecretTracingDisabled_WritesMaskedValues()
        {
            const string mask = "********";
            const string secret1 = "foo";
            const string secret2 = "bar";
            const string secret3 = "test";

            var sb = new StringBuilder();
            var listener = new StringWriter(sb);

            var trace = new Trace();
            trace.AddListener(listener);
            trace.IsSecretTracingEnabled = false;

            trace.WriteLineSecrets("Secrets: {0} {1} {2}", new object[]{ secret1, secret2, secret3 });

            string expectedTraceEnd = $"Secrets: {mask} {mask} {mask}\n";
            string actualTrace = sb.ToString();

            Assert.EndsWith(expectedTraceEnd, actualTrace, StringComparison.Ordinal);
        }
    }
}
