using System;

namespace GitCredentialManager
{
    public static class Base64UrlConvert
    {

        // The base64url format is the same as regular base64 format except:
        //   1. character 62 is "-" (minus) not "+" (plus)
        //   2. character 63 is "_" (underscore) not "/" (slash)
        //   3. padding is optional
        private const char base64PadCharacter = '=';
        private const char base64Character62 = '+';
        private const char base64Character63 = '/';
        private const char base64UrlCharacter62 = '-';
        private const char base64UrlCharacter63 = '_';

        public static string Encode(byte[] data, bool includePadding = true)
        {
            string base64Url = Convert.ToBase64String(data)
                .Replace(base64Character62, base64UrlCharacter62)
                .Replace(base64Character63, base64UrlCharacter63);

            return includePadding ? base64Url : base64Url.TrimEnd(base64PadCharacter);
        }

        public static byte[] Decode(string data)
        {
            string base64 = data
                .Replace(base64UrlCharacter62, base64Character62)
                .Replace(base64UrlCharacter63, base64Character63);

            switch (base64.Length % 4)
            {
                case 2:
                    base64 += base64PadCharacter;
                    goto case 3;
                case 3:
                    base64 += base64PadCharacter;
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
