using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitCredentialManager.Diagnostics
{
    public class CredentialStoreDiagnostic : Diagnostic
    {
        public CredentialStoreDiagnostic(ICommandContext commandContext)
            : base("Credential storage", commandContext)
        { }

        protected override Task<bool> RunInternalAsync(StringBuilder log, IList<string> additionalFiles)
        {
            log.AppendLine($"ICredentialStore instance is: {CommandContext.CredentialStore}");
            log.AppendLine($"CanStorePasswordExpiry: {CommandContext.CredentialStore.CanStorePasswordExpiry}");
            log.AppendLine($"CanStoreOAuthRefreshToken: {CommandContext.CredentialStore.CanStoreOAuthRefreshToken}");

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string account = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]
            var credential = new GitCredential(account, password) {
                OAuthRefreshToken = "xyzzy",
                PasswordExpiry = DateTimeOffset.FromUnixTimeSeconds(2147482647),
            };

            try
            {
                log.Append("Writing test credential...");
                CommandContext.CredentialStore.AddOrUpdate(service, credential);
                log.AppendLine(" OK");

                log.Append("Reading test credential...");
                ICredential outCredential = CommandContext.CredentialStore.Get(service, account);
                if (outCredential is null)
                {
                    log.AppendLine(" Failed");
                    log.AppendLine("Test credential object is null!");
                    return Task.FromResult(false);
                }

                log.AppendLine(" OK");

                if (!StringComparer.Ordinal.Equals(account, outCredential.Account))
                {
                    log.Append("Test credential account did not match!");
                    log.AppendLine($"Expected: {account}");
                    log.AppendLine($"Actual: {outCredential.Account}");
                    return Task.FromResult(false);
                }

                if (!StringComparer.Ordinal.Equals(password, outCredential.Password))
                {
                    log.Append("Test credential password did not match!");
                    log.AppendLine($"Expected: {password}");
                    log.AppendLine($"Actual: {outCredential.Password}");
                    return Task.FromResult(false);
                }

                if (CommandContext.CredentialStore.CanStorePasswordExpiry && !StringComparer.Ordinal.Equals(credential.PasswordExpiry, outCredential.PasswordExpiry))
                {
                    log.Append("Test credential password_expiry_utc did not match!");
                    log.AppendLine($"Expected: {credential.PasswordExpiry}");
                    log.AppendLine($"Actual: {outCredential.PasswordExpiry}");
                    return Task.FromResult(false);
                }

                if (CommandContext.CredentialStore.CanStoreOAuthRefreshToken && !StringComparer.Ordinal.Equals(credential.OAuthRefreshToken, outCredential.OAuthRefreshToken))
                {
                    log.Append("Test credential oauth_refresh_token did not match!");
                    log.AppendLine($"Expected: {credential.OAuthRefreshToken}");
                    log.AppendLine($"Actual: {outCredential.OAuthRefreshToken}");
                    return Task.FromResult(false);
                }                
            }
            finally
            {
                log.Append("Deleting test credential...");
                CommandContext.CredentialStore.Remove(service, credential);
                log.AppendLine(" OK");
            }

            return Task.FromResult(true);
        }
    }
}
