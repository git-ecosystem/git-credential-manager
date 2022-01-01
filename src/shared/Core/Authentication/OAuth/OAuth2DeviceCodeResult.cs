using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public record OAuth2DeviceCodeResult
    {
        public OAuth2DeviceCodeResult(string deviceCode, string userCode, Uri verificationUri, TimeSpan? interval, TimeSpan? expiresIn = null)
        {
            DeviceCode = deviceCode;
            UserCode = userCode;
            VerificationUri = verificationUri;
            PollingInterval = interval ?? TimeSpan.FromSeconds(5);
            ExpiresIn = expiresIn;
        }

        public string DeviceCode { get; }

        public string UserCode { get; }

        public Uri VerificationUri { get; }

        public TimeSpan PollingInterval { get; }

        public TimeSpan? ExpiresIn { get; }
    }
}
