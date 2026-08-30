using System;

namespace LastRail.Auth
{
    public sealed class AndroidKeystoreEncryptedTokenPayload
    {
        public AndroidKeystoreEncryptedTokenPayload(string cipherTextBase64, string ivBase64)
        {
            CipherTextBase64 = RequireText(cipherTextBase64, nameof(cipherTextBase64));
            IvBase64 = RequireText(ivBase64, nameof(ivBase64));
        }

        public string CipherTextBase64 { get; }
        public string IvBase64 { get; }

        private static string RequireText(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value.Trim();
        }
    }
}
