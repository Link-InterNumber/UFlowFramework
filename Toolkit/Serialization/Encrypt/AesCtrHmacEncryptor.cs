using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    /// <summary>
    /// AES-CTR + HMAC-SHA256 的认证加密工具。
    ///
    /// 数据格式：
    /// [1 byte Version][16 bytes Nonce][N bytes CipherText][32 bytes HMAC-SHA256 Tag]
    ///
    /// HMAC 覆盖 Version + Nonce + CipherText。
    /// 解密前必须先验证 HMAC，认证失败时绝不输出明文。
    /// </summary>
    public static class AesCtrHmacEncryptor
    {
        private const byte CurrentVersion = 1;

        private const int VersionLength = 1;
        private const int NonceLength = 16;
        private const int TagLength = 32;
        private const int BlockLength = 16;
        private const int DerivedKeyLength = 32;

        private const int HeaderLength = VersionLength + NonceLength;
        private const int OverheadLength = HeaderLength + TagLength;

        private static readonly byte[] EncryptionLabel = Encoding.ASCII.GetBytes("aes-ctr");
        private static readonly byte[] AuthenticationLabel = Encoding.ASCII.GetBytes("hmac-sha256");

        /*
         * 不直接用原始 encryptionKey 作为 Dictionary key，
         * 避免原始口令以字符串形式长期保存在静态缓存中。
         *
         * 缓存键包含 Encoding.CodePage，确保同一字符串经不同 Encoding
         * 转换为不同字节时，不会错误复用已派生的密钥材料。
         */
        private static readonly ConcurrentDictionary<string, KeyMaterial> KeyCache =
            new ConcurrentDictionary<string, KeyMaterial>();

        public static string Encrypt(string plainText, string encryptionKey, Encoding encoding)
        {
            // 保持原有兼容语义：未设置 key 时不加密。
            if (string.IsNullOrEmpty(encryptionKey))
                return plainText;

            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));

            ValidateEncoding(encoding);

            int byteCount = encoding.GetByteCount(plainText);
            byte[] rentedPlainBytes = ArrayPool<byte>.Shared.Rent(Math.Max(1, byteCount));

            try
            {
                int written = encoding.GetBytes(plainText.AsSpan(), rentedPlainBytes.AsSpan(0, byteCount));
                byte[] encryptedBytes = Encrypt(rentedPlainBytes.AsSpan(0, written), encryptionKey, encoding);

                try
                {
                    return Convert.ToBase64String(encryptedBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(encryptedBytes);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedPlainBytes.AsSpan(0, Math.Max(1, byteCount)));
                ArrayPool<byte>.Shared.Return(rentedPlainBytes);
            }
        }

        public static string Decrypt(string cipherText, string encryptionKey, Encoding encoding)
        {
            // 保持原有兼容语义：未设置 key 时不解密。
            if (string.IsNullOrEmpty(encryptionKey))
                return cipherText;

            if (cipherText == null)
                throw new ArgumentNullException(nameof(cipherText));

            ValidateEncoding(encoding);

            int maxCipherByteCount;
            try
            {
                maxCipherByteCount = checked((cipherText.Length / 4) * 3 + 2);
            }
            catch (OverflowException exception)
            {
                throw new FormatException("Cipher text is too large.", exception);
            }

            byte[] rentedCipherBytes = ArrayPool<byte>.Shared.Rent(Math.Max(1, maxCipherByteCount));

            try
            {
                if (!Convert.TryFromBase64String(cipherText, rentedCipherBytes, out int cipherByteCount))
                {
                    throw new FormatException("Invalid Base64 cipher text.");
                }

                byte[] plainBytes = Decrypt(rentedCipherBytes.AsSpan(0, cipherByteCount), encryptionKey, encoding);

                try
                {
                    return encoding.GetString(plainBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plainBytes);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedCipherBytes.AsSpan(0, Math.Max(1, maxCipherByteCount)));
                ArrayPool<byte>.Shared.Return(rentedCipherBytes);
            }
        }

        public static byte[] Encrypt(byte[] plainData, string encryptionKey, Encoding encoding)
        {
            if (plainData == null)
                throw new ArgumentNullException(nameof(plainData));

            return Encrypt(plainData.AsSpan(), encryptionKey, encoding);
        }

        public static byte[] Encrypt(ReadOnlySpan<byte> plainData, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                return plainData.ToArray();

            ValidateEncoding(encoding);

            byte[] result = new byte[GetEncryptedLength(plainData.Length)];

            try
            {
                if (!TryEncrypt(plainData, result, encryptionKey, encoding, out int written))
                    throw new CryptographicException("Encrypt destination buffer too small.");

                if (written != result.Length)
                    throw new CryptographicException("Unexpected encrypted length.");

                return result;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(result);
                throw;
            }
        }

        public static byte[] Decrypt(byte[] encryptData, string encryptionKey, Encoding encoding)
        {
            if (encryptData == null)
                throw new ArgumentNullException(nameof(encryptData));

            return Decrypt(encryptData.AsSpan(), encryptionKey, encoding);
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> encryptData, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                return encryptData.ToArray();

            ValidateEncoding(encoding);

            int plainLength = GetDecryptedLength(encryptData.Length);
            byte[] result = new byte[plainLength];

            try
            {
                if (!TryDecrypt(encryptData, result, encryptionKey, encoding, out int written))
                    throw new CryptographicException("Decrypt destination buffer too small.");

                if (written != result.Length)
                    throw new CryptographicException("Unexpected decrypted length.");

                return result;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(result);
                throw;
            }
        }

        public static bool TryEncrypt(ReadOnlySpan<byte> plainData, Span<byte> destination, string encryptionKey, Encoding encoding, out int written)
        {
            written = 0;
            if (string.IsNullOrEmpty(encryptionKey))
            {
                if (destination.Length < plainData.Length) return false;
                EnsureNoInvalidOverlap(plainData, destination.Slice(0, plainData.Length));
                plainData.CopyTo(destination);
                written = plainData.Length;
                return true;
            }
            ValidateEncoding(encoding);
            int requiredLength = GetEncryptedLength(plainData.Length);
            if (destination.Length < requiredLength)
                return false;

            EnsureNoInvalidOverlap(plainData, destination.Slice(0, requiredLength));
            KeyMaterial keyMaterial = GetKeyMaterial(encryptionKey, encoding);
            destination[0] = CurrentVersion;
            Span<byte> nonce = destination.Slice(VersionLength, NonceLength);
            RandomNumberGenerator.Fill(nonce);
            Span<byte> cipherText = destination.Slice(HeaderLength, plainData.Length);
            ApplyCtr(plainData, cipherText, keyMaterial.EncryptionKey, nonce);
            Span<byte> tag = destination.Slice(HeaderLength + plainData.Length, TagLength);
            ComputeTag(destination.Slice(0, HeaderLength + plainData.Length), tag, keyMaterial.AuthenticationKey);
            written = requiredLength;
            return true;
        }

        public static bool TryDecrypt(ReadOnlySpan<byte> encryptData, Span<byte> destination, string encryptionKey, Encoding encoding, out int written)
        {
            written = 0;

            if (string.IsNullOrEmpty(encryptionKey))
            {
                if (destination.Length < encryptData.Length)
                    return false;

                EnsureNoInvalidOverlap(encryptData, destination.Slice(0, encryptData.Length));
                encryptData.CopyTo(destination);
                written = encryptData.Length;
                return true;
            }

            ValidateEncoding(encoding);
            ValidatePayload(encryptData);

            int plainLength = GetDecryptedLength(encryptData.Length);
            if (destination.Length < plainLength)
                return false;

            EnsureNoInvalidOverlap(encryptData, destination.Slice(0, plainLength));
            KeyMaterial keyMaterial = GetKeyMaterial(encryptionKey, encoding);
            ReadOnlySpan<byte> headerAndCipherText = encryptData.Slice(0, encryptData.Length - TagLength);
            ReadOnlySpan<byte> expectedTag = encryptData.Slice(encryptData.Length - TagLength, TagLength);
            byte[] rentedTag = ArrayPool<byte>.Shared.Rent(TagLength);
            try
            {
                Span<byte> computedTag = rentedTag.AsSpan(0, TagLength);

                ComputeTag(headerAndCipherText, computedTag, keyMaterial.AuthenticationKey);
                if (!CryptographicOperations.FixedTimeEquals(computedTag, expectedTag))
                {
                    throw new CryptographicException("AES-CTR-HMAC authentication failed. The key is incorrect or the encrypted data was modified.");
                }
                ReadOnlySpan<byte> nonce = encryptData.Slice(VersionLength, NonceLength);
                ReadOnlySpan<byte> cipherText = encryptData.Slice(HeaderLength, plainLength);

                ApplyCtr(cipherText, destination.Slice(0, plainLength), keyMaterial.EncryptionKey, nonce);
                written = plainLength;
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedTag.AsSpan(0, TagLength));
                ArrayPool<byte>.Shared.Return(rentedTag);
            }
        }

        private static int GetEncryptedLength(int plainLength)
        {
            if (plainLength < 0)
                throw new ArgumentOutOfRangeException(nameof(plainLength));

            try
            {
                return checked(OverheadLength + plainLength);
            }
            catch (OverflowException exception)
            {
                throw new ArgumentOutOfRangeException($"Plain data is too large to encrypt, length = {plainLength}.", exception);
            }
        }

        private static int GetDecryptedLength(int encryptLength)
        {
            if (encryptLength < OverheadLength)
            {
                throw new CryptographicException($"Invalid AES-CTR-HMAC payload. Minimum length is {OverheadLength} bytes.");
            }

            return encryptLength - OverheadLength;
        }

        private static void ValidatePayload(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < OverheadLength)
            {
                throw new CryptographicException($"Invalid AES-CTR-HMAC payload. Minimum length is {OverheadLength} bytes.");
            }

            if (payload[0] != CurrentVersion)
            {
                throw new CryptographicException($"Unsupported AES-CTR-HMAC payload version: {payload[0]}.");
            }
        }

        private static void ValidateEncoding(Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
        }

        /// <summary>
        /// 只允许完全同位置的 in-place 操作。
        /// 错位重叠会导致 CTR 写入覆盖后续仍需读取的输入数据。
        /// </summary>
        private static void EnsureNoInvalidOverlap(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (source.Overlaps(destination, out int elementOffset) &&
                elementOffset != 0)
            {
                throw new ArgumentException("Source and destination cannot overlap at different offsets.");
            }
        }

        private static void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output, byte[] encryptionKey, ReadOnlySpan<byte> nonce)
        {
            if (nonce.Length != NonceLength)
                throw new ArgumentException($"Nonce must be {NonceLength} bytes.", nameof(nonce));

            if (output.Length < input.Length)
                throw new ArgumentException("Output buffer is too small.", nameof(output));

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.KeySize = 256;
                aes.Key = encryptionKey;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] rentedCounter = ArrayPool<byte>.Shared.Rent(BlockLength);
                    byte[] rentedKeyStream = ArrayPool<byte>.Shared.Rent(BlockLength);

                    try
                    {
                        Span<byte> counter = rentedCounter.AsSpan(0, BlockLength);
                        Span<byte> keyStream = rentedKeyStream.AsSpan(0, BlockLength);

                        nonce.CopyTo(counter);

                        int offset = 0;
                        while (offset < input.Length)
                        {
                            int transformed = encryptor.TransformBlock(rentedCounter, 0, BlockLength, rentedKeyStream, 0);

                            if (transformed != BlockLength)
                            {
                                throw new CryptographicException("Failed to generate AES-CTR keystream block.");
                            }

                            int take = Math.Min(BlockLength, input.Length - offset);

                            for (int i = 0; i < take; i++)
                            {
                                output[offset + i] = (byte)(input[offset + i] ^ keyStream[i]);
                            }

                            IncrementCounter(counter);
                            offset += take;
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(rentedCounter.AsSpan(0, BlockLength));
                        CryptographicOperations.ZeroMemory(rentedKeyStream.AsSpan(0, BlockLength));

                        ArrayPool<byte>.Shared.Return(rentedCounter);
                        ArrayPool<byte>.Shared.Return(rentedKeyStream);
                    }
                }
            }
        }

        private static void IncrementCounter(Span<byte> counter)
        {
            for (int i = counter.Length - 1; i >= 0; i--)
            {
                counter[i]++;

                if (counter[i] != 0)
                    return;
            }

            // 理论上需要处理 2^128 个 AES block 才可能走到这里。
            // 但显式抛错比复用 keystream 更安全。
            throw new CryptographicException("AES-CTR counter overflow.");
        }

        private static void ComputeTag(ReadOnlySpan<byte> data, Span<byte> destination, byte[] authenticationKey)
        {
            if (destination.Length < TagLength)
                throw new ArgumentException("Destination buffer is too small.", nameof(destination));

            using (HMACSHA256 hmac = new HMACSHA256(authenticationKey))
            {
                if (!hmac.TryComputeHash(data, destination, out int written) || written != TagLength)
                {
                    throw new CryptographicException("Failed to compute HMAC-SHA256.");
                }
            }
        }

        private static KeyMaterial GetKeyMaterial(string encryptionKey, Encoding encoding)
        {
            ValidateEncoding(encoding);

            string cacheKey = GetKeyCacheKey(encryptionKey, encoding);

            return KeyCache.GetOrAdd(cacheKey, _ => DeriveKeyMaterial(encryptionKey, encoding));
        }

        private static string GetKeyCacheKey(string encryptionKey, Encoding encoding)
        {
            byte[] sourceBytes = encoding.GetBytes(encryptionKey);
            byte[] hash;

            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(sourceBytes);
                }

                try
                {
                    return $"{encoding.CodePage}:{Convert.ToBase64String(hash)}";
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(hash);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sourceBytes);
            }
        }

        private static KeyMaterial DeriveKeyMaterial(string encryptionKey, Encoding encoding)
        {
            int byteCount = encoding.GetByteCount(encryptionKey);
            byte[] rentedKeyBytes = ArrayPool<byte>.Shared.Rent(Math.Max(1, byteCount));

            try
            {
                int written = encoding.GetBytes(encryptionKey.AsSpan(), rentedKeyBytes.AsSpan(0, byteCount));

                byte[] masterKey;

                using (SHA256 sha256 = SHA256.Create())
                {
                    masterKey = sha256.ComputeHash(rentedKeyBytes, 0, written);
                }

                try
                {
                    byte[] encryptionSubKey;
                    byte[] authenticationSubKey;

                    using (HMACSHA256 hmac = new HMACSHA256(masterKey))
                    {
                        encryptionSubKey = hmac.ComputeHash(EncryptionLabel);
                        authenticationSubKey = hmac.ComputeHash(AuthenticationLabel);
                    }

                    if (encryptionSubKey.Length != DerivedKeyLength ||
                        authenticationSubKey.Length != DerivedKeyLength)
                    {
                        CryptographicOperations.ZeroMemory(encryptionSubKey);
                        CryptographicOperations.ZeroMemory(authenticationSubKey);

                        throw new CryptographicException("Unexpected derived key length.");
                    }

                    return new KeyMaterial(
                        encryptionSubKey,
                        authenticationSubKey);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(masterKey);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedKeyBytes.AsSpan(0, Math.Max(1, byteCount)));
                ArrayPool<byte>.Shared.Return(rentedKeyBytes);
            }
        }

        private sealed class KeyMaterial
        {
            public KeyMaterial(byte[] encryptionKey, byte[] authenticationKey)
            {
                EncryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
                AuthenticationKey = authenticationKey ?? throw new ArgumentNullException(nameof(authenticationKey));
            }

            public byte[] EncryptionKey { get; }

            public byte[] AuthenticationKey { get; }
        }
    }
}