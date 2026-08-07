using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace PowerCellStudio
{
    /// <summary>
    /// 跨 Unity 平台的 AES-GCM 加解密。
    ///
    /// 输出格式：
    /// [12 字节 Nonce][密文][16 字节 Authentication Tag]
    /// </summary>
    public static class AesGcmEncryptor
    {
        private const int NonceLength = 12;
        private const int TagLength = 16;
        private const int TagLengthBits = TagLength * 8;

        public static byte[] Encrypt(ReadOnlySpan<byte> plainData, string encryptionKey, Encoding encoding)
        {
            if (encryptionKey == null)
                throw new ArgumentNullException(nameof(encryptionKey));

            byte[] key = encoding.GetBytes(encryptionKey);
            ValidateAesKey(key);

            byte[] nonce = new byte[NonceLength];
            RandomNumberGenerator.Fill(nonce);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), TagLengthBits, nonce);

            cipher.Init(true, parameters);

            byte[] input = plainData.ToArray();
            byte[] cipherAndTag = new byte[cipher.GetOutputSize(input.Length)];

            int written = cipher.ProcessBytes(input, 0, input.Length, cipherAndTag, 0);

            written += cipher.DoFinal(cipherAndTag, written);

            byte[] encryptedData = new byte[NonceLength + written];
            Buffer.BlockCopy(nonce, 0, encryptedData, 0, NonceLength);
            Buffer.BlockCopy(cipherAndTag, 0, encryptedData, NonceLength, written);

            return encryptedData;
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> encryptData, string encryptionKey, Encoding encoding)
        {
            if (encryptionKey == null)
                throw new ArgumentNullException(nameof(encryptionKey));

            if (encryptData.Length < NonceLength + TagLength)
            {
                throw new CryptographicException($"Invalid AES-GCM payload. It must contain at least {NonceLength} bytes of nonce and {TagLength} bytes of tag.");
            }

            byte[] key = encoding.GetBytes(encryptionKey);
            ValidateAesKey(key);

            byte[] encryptedBytes = encryptData.ToArray();

            byte[] nonce = new byte[NonceLength];
            Buffer.BlockCopy(encryptedBytes, 0, nonce, 0, NonceLength);

            int cipherAndTagLength = encryptedBytes.Length - NonceLength;
            byte[] cipherAndTag = new byte[cipherAndTagLength];
            Buffer.BlockCopy(encryptedBytes, NonceLength, cipherAndTag, 0, cipherAndTagLength);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), TagLengthBits, nonce);

            cipher.Init(false, parameters);

            try
            {
                byte[] plainData = new byte[cipher.GetOutputSize(cipherAndTag.Length)];

                int written = cipher.ProcessBytes(cipherAndTag, 0, cipherAndTag.Length, plainData, 0);

                written += cipher.DoFinal(plainData, written);

                if (written == plainData.Length)
                {
                    return plainData;
                }

                byte[] result = new byte[written];
                Buffer.BlockCopy(plainData, 0, result, 0, written);
                return result;
            }
            catch (InvalidCipherTextException exception)
            {
                // 统一转换为 .NET 的加密异常，方便现有测试捕获。
                throw new CryptographicException("AES-GCM authentication failed. The key is incorrect or the encrypted data was modified.", exception);
            }
        }

        public static string Encrypt(string plainText, string encryptionKey, Encoding encoding)
        {
            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));

            return Convert.ToBase64String(Encrypt(encoding.GetBytes(plainText), encryptionKey, encoding));
        }

        public static string Decrypt(string cipherText, string encryptionKey, Encoding encoding)
        {
            if (cipherText == null)
                throw new ArgumentNullException(nameof(cipherText));

            return encoding.GetString(Decrypt(Convert.FromBase64String(cipherText), encryptionKey, encoding));
        }

        private static void ValidateAesKey(byte[] key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new ArgumentException($"AES key must be exactly 16, 24, or 32 bytes after encoding. Current length: {key.Length}.", nameof(key));
            }
        }
    }
}