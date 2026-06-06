using System;
using System.Security.Cryptography;
using System.Text;

namespace K3CloudDataDictionary.Helpers
{
    public static class PasswordHelper
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("K3CloudDataDictionary_Pwd_Protection_v1");

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            try
            {
                var encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(plainText),
                    Entropy,
                    DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                return plainText;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return encryptedText;
            try
            {
                var decrypted = ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedText),
                    Entropy,
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return encryptedText;
            }
        }
    }
}
