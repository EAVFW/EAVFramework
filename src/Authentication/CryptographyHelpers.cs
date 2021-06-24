using System;
using System.Security.Cryptography;

namespace DotNetDevOps.Extensions.EAVFramework.Authentication
{
    public static class CryptographyHelpers
    {
        
        public static Guid CreateCryptographicallySecureGuid()
        {
            using var provider = new RNGCryptoServiceProvider();
            var bytes = new byte[16];
            provider.GetBytes(bytes);

            return new Guid(bytes);
        }

        public static byte[] Decrypt(string password, string salt, byte[] encrypted_bytes)
        {

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    var keys = GetAesKeyAndIV(password, salt, aes);
                    aes.Key = keys.Item1;
                    aes.IV = keys.Item2;

                    // create a decryptor to perform the stream transform.
                    var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    return decryptor.TransformFinalBlock(encrypted_bytes, 0, encrypted_bytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{password} {salt} {Convert.ToBase64String(encrypted_bytes)}  {ex.Message}", ex);
            }


        }

        public static byte[] Encrypt(string password, string salt, byte[] payload)
        {

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var keys = GetAesKeyAndIV(password, salt, aes);
                    aes.Key = keys.Item1;
                    aes.IV = keys.Item2;

                    var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    var encrypted = encryptor.TransformFinalBlock(payload, 0, payload.Length);
                    return encrypted;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{password} {salt} {ex.Message}", ex);
            }

        }

        private static byte[] ToByteArray(string input)
        {
            return Convert.FromBase64String(input);
        }

        private static string ToString(byte[] input)
        {
            return Convert.ToBase64String(input);
        }

        private static Tuple<byte[], byte[]> GetAesKeyAndIV(string password, string salt, SymmetricAlgorithm symmetricAlgorithm)
        {
            const int bits = 8;
            var key = new byte[16];
            var iv = new byte[16];

            var derive_bytes = new Rfc2898DeriveBytes(password, ToByteArray(salt));
            key = derive_bytes.GetBytes(symmetricAlgorithm.KeySize / bits);
            iv = derive_bytes.GetBytes(symmetricAlgorithm.BlockSize / bits);

            return new Tuple<byte[], byte[]>(key, iv);
        }

    }
}