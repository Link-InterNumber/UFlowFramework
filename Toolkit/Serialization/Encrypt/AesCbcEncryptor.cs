using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public class AesCbcEncryptor 
    {
        private const int AesKeyLength = 16;
        private static readonly Dictionary<string, byte[]> AesKeyCache = new Dictionary<string, byte[]>();

        public static byte[] AESEncrypt(byte[] data, string encryptionKey, Encoding encoding, int offset = 0, int count = -1)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return data;
            try
            {
                return EncryptAesBytesCore(data, encryptionKey, encoding, offset, count);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return data;
            }
        }
        
        public static byte[] AESDecrypt(byte[] encryptData, string encryptionKey, Encoding encoding, int offset = 0, int count = -1)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return encryptData;
            try
            {
                return DecryptAesBytesCore(encryptData, encryptionKey, encoding, offset, count);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return encryptData;
            }
        }

        public static string AESEncrypt(string plainText, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return plainText;
            byte[] plainBytes = encoding.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesBytesCore(plainBytes, encryptionKey, encoding);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string AESDecrypt(string cipherText, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return cipherText;
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] resultBytes = DecryptAesBytesCore(cipherBytes, encryptionKey, encoding);
            return encoding.GetString(resultBytes, 0, resultBytes.Length);
        }

        private static byte[] EncryptAesBytesCore(byte[] data, string encryptionKey, Encoding encoding, int offset = 0, int count = -1)
        {
            using (Aes aes = CreateAes(encryptionKey, encoding))
            {
                aes.GenerateIV();
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] cipherBytes = encryptor.TransformFinalBlock(data, offset, count == -1 ? data.Length : count);
                    byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
                    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
                    return result;
                }
            }
        }

        private static byte[] DecryptAesBytesCore(byte[] encryptData, string encryptionKey, Encoding encoding, int offset = 0, int count = -1)
        {
            using (Aes aes = CreateAes(encryptionKey, encoding))
            {
                int ivLength = aes.BlockSize >> 3;
                if (encryptData == null || encryptData.Length < ivLength + offset)
                    throw new CryptographicException("Invalid AES payload.");

                byte[] iv = new byte[ivLength];
                Buffer.BlockCopy(encryptData, offset, iv, 0, ivLength);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptData, ivLength + offset, (count == -1 ? encryptData.Length : count) - ivLength);
                }
            }
        }

        private static Aes CreateAes(string encryptionKey, Encoding encoding)
        {
            Aes aes = Aes.Create();
            aes.Key = GetAesKeyBytes(encryptionKey, encoding);
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private static byte[] GetAesKeyBytes(string encryptionKey, Encoding encoding)
        {
            if (AesKeyCache.TryGetValue(encryptionKey, out byte[] cachedKey))
            {
                return cachedKey;
            }
            else
            {
                byte[] normalizedKey = new byte[AesKeyLength];
                byte[] sourceBytes = encoding.GetBytes(encryptionKey);
                int copyLength = Math.Min(sourceBytes.Length, AesKeyLength);
                Buffer.BlockCopy(sourceBytes, 0, normalizedKey, 0, copyLength);
                AesKeyCache[encryptionKey] = normalizedKey;
                return normalizedKey;
            }
        }
    }
}