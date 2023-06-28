using System;
using System.Security.Cryptography;
using System.Text;

namespace GitCredentialManager.Authentication.OAuth
{
    public enum OAuth2PkceChallengeMethod
    {
        Plain = 0,
        Sha256 = 1
    }

    public interface IOAuth2CodeGenerator
    {
        /// <summary>
        /// Create a random string value suitable for use as a nonce.
        /// </summary>
        /// <returns>A random string.</returns>
        string CreateNonce();

        /// <summary>
        /// Create a cryptographically random code verifier for use with Proof Key for Code Exchange (PKCE).
        /// </summary>
        /// <returns>PKCE code verifier.</returns>
        string CreatePkceCodeVerifier();

        /// <summary>
        /// Create a code challenge for the given Proof Key for Code Exchange (PKCE) code verifier.
        /// </summary>
        /// <param name="challengeMethod">Challenge method.</param>
        /// /// <param name="codeVerifier">PKCE code verifier.</param>
        /// <returns>PKCE code challenge.</returns>
        string CreatePkceCodeChallenge(OAuth2PkceChallengeMethod challengeMethod, string codeVerifier);
    }

    public class OAuth2CryptographicCodeGenerator : IOAuth2CodeGenerator
    {
        // Do not include padding of the base64url string to avoid percent-encoding when passed in a URI
        private const bool PkceIncludeBase64UrlPadding = false;

        public string CreateNonce()
        {
            return Guid.NewGuid().ToString("N");
        }

        public string CreatePkceCodeVerifier()
        {
            //
            // RFC 7636 requires the code verifier to match the following ABNF:
            //
            //   code-verifier = 43*128unreserved
            //   unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
            //   ALPHA = %x41-5A / %x61-7A
            //   DIGIT = %x30-39
            //
            // The base64url encoding (RFC 6749) is a subset of these characters
            // so we opt to convert our random bytes into Base64URL format for
            // the code verifier.
            //
            // RFC 7636 mandates the code verifier must be between 43 and 128 characters
            // long (inclusive). We want to generate a string at the top end of this range
            // for maximum entropy. At the same time we want to avoid using the padding
            // character '=' because this character is percent-encoded when used in URLs.
            // To avoid padding we need the number of input bytes to be divisible by 3.
            //
            // In order to achieve 128 base64url characters AND avoid padding we should
            // generate exactly 96 random bytes. Why 96 bytes? 96 is divisible by 3 and:
            //
            //   96 bytes -> 768 bits -> 128 base64url characters (6 bits per character)
            //
            var buf = new byte[96];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buf);

            return Base64UrlConvert.Encode(buf, PkceIncludeBase64UrlPadding);
        }

        public string CreatePkceCodeChallenge(OAuth2PkceChallengeMethod challengeMethod, string codeVerifier)
        {
            switch (challengeMethod)
            {
                case OAuth2PkceChallengeMethod.Plain:
                    return codeVerifier;

                case OAuth2PkceChallengeMethod.Sha256:
                    // The "S256" code challenge is computed as follows, per RFC 7636:
                    //
                    //   code_challenge = BASE64URL-ENCODE(SHA256(ASCII(code_verifier)))
                    //
                    using (var sha256 = SHA256.Create())
                    {
                        return Base64UrlConvert.Encode(
                            sha256.ComputeHash(
                                Encoding.ASCII.GetBytes(codeVerifier)
                            ),
                            PkceIncludeBase64UrlPadding
                        );
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(challengeMethod), challengeMethod, "Unknown PKCE code challenge method.");
            }
        }
    }
}
