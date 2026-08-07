using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    /// <summary>
    /// AES-CBC 加密工具。
    ///
    /// 新格式：
    /// [Magic(8)][IV(16)][Ciphertext(N * 16)][HMAC-SHA256(32)]
    ///
    /// 旧格式：
    /// [IV(16)][Ciphertext(N * 16)]
    ///
    /// 解密时同时兼容旧格式；加密时始终输出带 HMAC 的新格式。
    /// </summary>
    public class AesCbcEncryptor
    {
        private const int AesKeyLength = 16;
        private const int IvLength = 16;
        private const int MacLength = 32;

        // 8 字节标识，降低旧格式随机 IV 被误识别为新格式的概率。
        private static readonly byte[] PayloadMagic =
        {
            (byte)'P', (byte)'C', (byte)'S', (byte)'A',
            (byte)'E', (byte)'S', (byte)'0', (byte)'1'
        };

        private static readonly byte[] MacKeyContext = Encoding.UTF8.GetBytes("PowerCellStudio.AesCbcEncryptor.MacKey.v1");

        // 原 key 派生规则保持与旧版本一致，保证旧数据仍然能被解密。
        private static readonly ConcurrentDictionary<string, byte[]> AesKeyCache = new ConcurrentDictionary<string, byte[]>();

        private static readonly ConcurrentDictionary<string, byte[]> MacKeyCache = new ConcurrentDictionary<string, byte[]>();

        public static byte[] AESEncrypt(byte[] data, string encryptionKey, Encoding encoding,
            int offset = 0, int count = -1)
        {
            // 与原实现保持一致：未配置 key 时不加密，直接返回原始数据。
            if (string.IsNullOrEmpty(encryptionKey)) return data;
            ValidateDataAndRange(data, offset, count, nameof(data));
            ValidateEncoding(encoding);
            return EncryptAesBytesCore(data, encryptionKey, encoding, offset, count);
        }

        public static byte[] AESDecrypt(byte[] encryptData, string encryptionKey, Encoding encoding,
            int offset = 0, int count = -1)
        {
            // 与原实现保持一致：未配置 key 时不解密，直接返回原始数据。
            if (string.IsNullOrEmpty(encryptionKey)) return encryptData;
            ValidateDataAndRange(encryptData, offset, count, nameof(encryptData));
            ValidateEncoding(encoding);
            return DecryptAesBytesCore(encryptData, encryptionKey, encoding, offset, count);
        }

        public static string AESEncrypt(string plainText, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return plainText;

            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));
            

            ValidateEncoding(encoding);
            byte[] plainBytes = encoding.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesBytesCore(plainBytes, encryptionKey, encoding);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string AESDecrypt(string cipherText, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                return cipherText;
            
            if (cipherText == null)
                throw new ArgumentNullException(nameof(cipherText));

            ValidateEncoding(encoding);

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] resultBytes = DecryptAesBytesCore(cipherBytes, encryptionKey, encoding);
            return encoding.GetString(resultBytes);
        }

        private static byte[] EncryptAesBytesCore(byte[] data, string encryptionKey,
            Encoding encoding, int offset = 0, int count = -1)
        {
            int plainDataLength = GetSelectedLength(data.Length, offset, count);
            byte[] aesKey = GetAesKeyBytes(encryptionKey, encoding);
            byte[] macKey = GetMacKeyBytes(encryptionKey, encoding);

            byte[] iv;
            byte[] cipherBytes;

            using (Aes aes = CreateAes(aesKey))
            {
                aes.GenerateIV();
                iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    cipherBytes = encryptor.TransformFinalBlock(data, offset, plainDataLength);
                }
            }

            // 新格式：[Magic][IV][Ciphertext][HMAC]
            int payloadWithoutMacLength = PayloadMagic.Length + IvLength + cipherBytes.Length;
            byte[] result = new byte[payloadWithoutMacLength + MacLength];

            Buffer.BlockCopy(PayloadMagic, 0, result, 0, PayloadMagic.Length);
            Buffer.BlockCopy(iv, 0, result, PayloadMagic.Length, IvLength);
            Buffer.BlockCopy(cipherBytes, 0, result, PayloadMagic.Length + IvLength, cipherBytes.Length);

            byte[] mac = CalculateHmac(result, 0, payloadWithoutMacLength, macKey);
            Buffer.BlockCopy(mac, 0, result, payloadWithoutMacLength, MacLength);

            return result;
        }

        private static byte[] DecryptAesBytesCore(byte[] encryptData, string encryptionKey,
            Encoding encoding, int offset = 0, int count = -1)
        {
            int selectedLength = GetSelectedLength(encryptData.Length, offset, count);
            byte[] aesKey = GetAesKeyBytes(encryptionKey, encoding);

            if (IsAuthenticatedPayload(encryptData, offset, selectedLength))
                return DecryptAuthenticatedPayload(encryptData, encryptionKey, encoding, offset, selectedLength, aesKey);
            // 兼容旧格式：[IV][Ciphertext]
            return DecryptLegacyPayload(encryptData, offset, selectedLength, aesKey);
        }

        private static byte[] DecryptAuthenticatedPayload(byte[] encryptData, string encryptionKey, Encoding encoding,
            int offset, int selectedLength, byte[] aesKey)
        {
            int minimumLength = PayloadMagic.Length + IvLength + IvLength + MacLength;
            if (selectedLength < minimumLength)
                throw new CryptographicException("Invalid authenticated AES payload length.");

            int macOffset = offset + selectedLength - MacLength;
            int contentLength = selectedLength - MacLength;
            int cipherLength = contentLength - PayloadMagic.Length - IvLength;

            if (cipherLength <= 0 || cipherLength % IvLength != 0)
                throw new CryptographicException("Invalid authenticated AES ciphertext length.");

            byte[] macKey = GetMacKeyBytes(encryptionKey, encoding);
            byte[] expectedMac = CalculateHmac(encryptData, offset, contentLength, macKey);

            if (!FixedTimeEquals(encryptData, macOffset, expectedMac, 0, MacLength))
                throw new CryptographicException("AES payload authentication failed. The data may be corrupted, tampered with, or encrypted with another key.");

            byte[] iv = new byte[IvLength];
            Buffer.BlockCopy(encryptData, offset + PayloadMagic.Length, iv, 0, IvLength);

            using (Aes aes = CreateAes(aesKey))
            {
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptData, offset + PayloadMagic.Length + IvLength, cipherLength);
                }
            }
        }

        private static byte[] DecryptLegacyPayload(byte[] encryptData, int offset, int selectedLength, byte[] aesKey)
        {
            // 旧 CBC 格式至少应包含 16-byte IV + 16-byte PKCS7 密文块。
            if (selectedLength < IvLength * 2)
                throw new CryptographicException("Invalid legacy AES payload length.");

            int cipherLength = selectedLength - IvLength;
            if (cipherLength % IvLength != 0)
                throw new CryptographicException("Invalid legacy AES ciphertext length.");

            byte[] iv = new byte[IvLength];
            Buffer.BlockCopy(encryptData, offset, iv, 0, IvLength);

            using (Aes aes = CreateAes(aesKey))
            {
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptData, offset + IvLength, cipherLength);
                }
            }
        }

        private static Aes CreateAes(byte[] key)
        {
            Aes aes = Aes.Create();

            if (aes == null)
                throw new CryptographicException("The current platform does not provide an AES implementation.");

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = AesKeyLength * 8;
            aes.Key = key;

            return aes;
        }

        /// <summary>
        /// 保持旧版本的 key 规范化方式：
        /// UTF-8 bytes 取前 16 字节，不足部分补 0。
        ///
        /// 不能直接改成 SHA256/PBKDF2，否则历史 AES-CBC 数据将无法解密。
        /// </summary>
        private static byte[] GetAesKeyBytes(string encryptionKey, Encoding encoding)
        {
            string cacheKey = GetCacheKey(encryptionKey, encoding);

            return AesKeyCache.GetOrAdd(cacheKey, _ =>
            {
                byte[] normalizedKey = new byte[AesKeyLength];
                byte[] sourceBytes = encoding.GetBytes(encryptionKey);
                int copyLength = Math.Min(sourceBytes.Length, AesKeyLength);
                Buffer.BlockCopy(sourceBytes, 0, normalizedKey, 0, copyLength);
                return normalizedKey;
            });
        }

        /// <summary>
        /// 使用独立的认证 key，避免直接重用 CBC 的 AES key 来计算 HMAC。
        /// </summary>
        private static byte[] GetMacKeyBytes(string encryptionKey, Encoding encoding)
        {
            string cacheKey = GetCacheKey(encryptionKey, encoding);
            return MacKeyCache.GetOrAdd(cacheKey, _ =>
            {
                byte[] aesKey = GetAesKeyBytes(encryptionKey, encoding);
                using (HMACSHA256 hmac = new HMACSHA256(aesKey))
                {
                    return hmac.ComputeHash(MacKeyContext);
                }
            });
        }

        private static byte[] CalculateHmac(byte[] data, int offset, int count, byte[] macKey)
        {
            using (HMACSHA256 hmac = new HMACSHA256(macKey))
            {
                return hmac.ComputeHash(data, offset, count);
            }
        }

        private static bool IsAuthenticatedPayload(byte[] data, int offset, int count)
        {
            if (count < PayloadMagic.Length)
            {
                return false;
            }

            for (int i = 0; i < PayloadMagic.Length; i++)
            {
                if (data[offset + i] != PayloadMagic[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 避免使用普通 SequenceEqual / == 进行 MAC 比较，
        /// 防止比较提前返回导致的时序信息泄露。
        /// </summary>
        private static bool FixedTimeEquals(byte[] left, int leftOffset, byte[] right, int rightOffset, int count)
        {
            if (left == null || right == null ||
                leftOffset < 0 || rightOffset < 0 ||
                count < 0 ||
                leftOffset > left.Length - count ||
                rightOffset > right.Length - count)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < count; i++)
            {
                difference |= left[leftOffset + i] ^ right[rightOffset + i];
            }
            return difference == 0;
        }

        private static void ValidateDataAndRange(byte[] data, int offset, int count, string parameterName)
        {
            if (data == null)
                throw new ArgumentNullException(parameterName);

            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset must be within the input byte array.");

            if (count < -1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be -1 or a non-negative number.");

            if (count != -1 && count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Offset and count must specify a valid range in the input byte array.");
        }

        private static void ValidateEncoding(Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
        }

        private static int GetSelectedLength(int totalLength, int offset, int count)
        {
            return count == -1 ? totalLength - offset : count;
        }

        private static string GetCacheKey(string encryptionKey, Encoding encoding)
        {
            // Encoding 也纳入缓存 key，避免同一字符串在不同编码下错误复用 key。
            return encoding.CodePage + "\0" + encryptionKey;
        }
    }
}