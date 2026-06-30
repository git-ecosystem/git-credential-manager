using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GitCredentialManager.Authentication.OAuth;
using Xunit;

namespace GitCredentialManager.Tests.Authentication
{
    public class OAuth2CryptographicCodeGeneratorTests
    {
        private const string ValidBase64UrlCharsNoPad = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        [Fact]
        public void OAuth2CryptographicCodeGenerator_CreateNonce_IsUnique()
        {
            var generator = new OAuth2CryptographicCodeGenerator();

            // Create a bunch of nonce values
            var nonces = new string[32];
            for (int i = 0; i < nonces.Length; i++)
            {
                nonces[i] = generator.CreateNonce();
            }

            // There should be no duplicates
            string[] uniqueNonces = nonces.Distinct().ToArray();
            Assert.Equal(uniqueNonces, nonces);
        }

        [Fact]
        public void OAuth2CryptographicCodeGenerator_CreatePkceCodeVerifier_IsUniqueBase64UrlStringWithoutPaddingAndLengthBetween43And128()
        {
            var generator = new OAuth2CryptographicCodeGenerator();

            // Create a bunch of verifiers
            var verifiers = new string[32];
            for (int i = 0; i < verifiers.Length; i++)
            {
                string v = generator.CreatePkceCodeVerifier();

                // Assert the verifier is a base64url string without padding
                char[] vs = v.ToCharArray();
                Assert.All(vs, x => Assert.Contains(x, ValidBase64UrlCharsNoPad));

                // Assert the verifier is a string of length [43, 128] (inclusive)
                Assert.InRange(v.Length, 43, 128);

                verifiers[i] = v;
            }

            // There should be no duplicates
            string[] uniqueVerifiers = verifiers.Distinct().ToArray();
            Assert.Equal(uniqueVerifiers, verifiers);
        }

        [Fact]
        public void OAuth2CryptographicCodeGenerator_CreatePkceCodeChallenge_Plain_ReturnsVerifierUnchanged()
        {
            var generator = new OAuth2CryptographicCodeGenerator();

            var verifier = generator.CreatePkceCodeVerifier();
            var challenge = generator.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Plain, verifier);

            Assert.Equal(verifier, challenge);
        }

        [Fact]
        public void OAuth2CryptographicCodeGenerator_CreatePkceCodeChallenge_Sha256_ReturnsBase64UrlEncodedSha256HashOfAsciiVerifier()
        {
            var generator = new OAuth2CryptographicCodeGenerator();

            var verifier = generator.CreatePkceCodeVerifier();

            byte[] verifierAsciiBytes = Encoding.ASCII.GetBytes(verifier);
            byte[] hashedBytes;
            using (var sha256 = SHA256.Create())
            {
                hashedBytes = sha256.ComputeHash(verifierAsciiBytes);
            }

            var expectedChallenge = Base64UrlConvert.Encode(hashedBytes, false);
            var actualChallenge = generator.CreatePkceCodeChallenge(OAuth2PkceChallengeMethod.Sha256, verifier);

            Assert.Equal(expectedChallenge, actualChallenge);
        }
    }
}
