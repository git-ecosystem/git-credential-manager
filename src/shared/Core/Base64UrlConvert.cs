using System;

namespace GitCredentialManager
{
    public static class Base64UrlConvert
    {
        public static string Encode(byte[] data, bool includePadding = true)
        {
            const char base64PadCharacter = '=';
            const char base64Character62 = '+';
            const char base64Character63 = '/';
            const char base64UrlCharacter62 = '-';
            const char base64UrlCharacter63 = '_';

            // The base64url format is the same as regular base64 format except:
            //   1. character 62 is "-" (minus) not "+" (plus)
            //   2. character 63 is "_" (underscore) not "/" (slash)
            string base64Url = Convert.ToBase64String(data)
                .Replace(base64Character62, base64UrlCharacter62)
                .Replace(base64Character63, base64UrlCharacter63);

            return includePadding ? base64Url : base64Url.TrimEnd(base64PadCharacter);
        }
    }
}
