using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2DeviceCodeResult
    {
        public OAuth2DeviceCodeResult(string deviceCode, string userCode, Uri verificationUri, TimeSpan? interval)
        {
            DeviceCode = deviceCode;
            UserCode = userCode;
            VerificationUri = verificationUri;
            PollingInterval = interval ?? TimeSpan.FromSeconds(5);
        }

        public string DeviceCode { get; }

        public string UserCode { get; }

        public Uri VerificationUri { get; }

        public TimeSpan PollingInterval { get; }

        public TimeSpan? ExpiresIn { get; internal set; }
    }
}
