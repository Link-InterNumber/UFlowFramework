using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public static class AesCtrHmacEncryptor
    {
        private const byte CurrentVersion = 1;
        private const int VersionLength = 1;
        private const int NonceLength = 16;
        private const int TagLength = 32;
        private const int BlockLength = 16;
        private const int DerivedKeyLength = 32;

        private static readonly byte[] EncryptionLabel = Encoding.ASCII.GetBytes("aes-ctr");
        private static readonly byte[] AuthenticationLabel = Encoding.ASCII.GetBytes("hmac-sha256");
        private static readonly ConcurrentDictionary<string, KeyMaterial> KeyCache = new ConcurrentDictionary<string, KeyMaterial>();

        public static string Encrypt(string plainText, string encryptionKey, Encoding encoding)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                return plainText;

            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

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
            if (string.IsNullOrEmpty(encryptionKey))
                return cipherText;

            if (cipherText == null)
                throw new ArgumentNullException(nameof(cipherText));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            int maxCipherByteCount = (cipherText.Length / 4) * 3 + 2;
            byte[] rentedCipherBytes = ArrayPool<byte>.Shared.Rent(Math.Max(1, maxCipherByteCount));

            try
            {
                if (!Convert.TryFromBase64String(cipherText, rentedCipherBytes, out int cipherByteCount))
                    throw new FormatException("Invalid Base64 cipher text.");

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

            byte[] result = new byte[GetEncryptedLength(plainData.Length)];
            if (!TryEncrypt(plainData, result, encryptionKey, encoding, out int written))
                throw new CryptographicException("Encrypt destination buffer too small.");

            if (written != result.Length)
                throw new CryptographicException("Unexpected encrypted length.");

            return result;
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

            int plainLength = GetDecryptedLength(encryptData.Length);
            byte[] result = new byte[plainLength];

            if (!TryDecrypt(encryptData, result, encryptionKey, encoding, out int written))
                throw new CryptographicException("Decrypt destination buffer too small.");

            if (written != result.Length)
                throw new CryptographicException("Unexpected decrypted length.");

            return result;
        }

        public static bool TryEncrypt(ReadOnlySpan<byte> plainData, Span<byte> destination, string encryptionKey, Encoding encoding, out int written)
        {
            written = 0;

            if (string.IsNullOrEmpty(encryptionKey))
            {
                if (destination.Length < plainData.Length)
                    return false;

                plainData.CopyTo(destination);
                written = plainData.Length;
                return true;
            }

            int requiredLength = GetEncryptedLength(plainData.Length);
            if (destination.Length < requiredLength)
                return false;

            KeyMaterial keyMaterial = GetKeyMaterial(encryptionKey, encoding);

            destination[0] = CurrentVersion;

            Span<byte> nonce = destination.Slice(VersionLength, NonceLength);
            RandomNumberGenerator.Fill(nonce);

            Span<byte> cipherText = destination.Slice(VersionLength + NonceLength, plainData.Length);
            ApplyCtr(plainData, cipherText, keyMaterial.EncryptionKey, nonce);

            Span<byte> tag = destination.Slice(VersionLength + NonceLength + plainData.Length, TagLength);
            ComputeTag(destination.Slice(0, VersionLength + NonceLength + plainData.Length), tag, keyMaterial.AuthenticationKey);

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

                encryptData.CopyTo(destination);
                written = encryptData.Length;
                return true;
            }

            ValidatePayload(encryptData);

            int plainLength = GetDecryptedLength(encryptData.Length);
            if (destination.Length < plainLength)
                return false;

            KeyMaterial keyMaterial = GetKeyMaterial(encryptionKey, encoding);

            ReadOnlySpan<byte> headerAndCipherText = encryptData.Slice(0, encryptData.Length - TagLength);
            ReadOnlySpan<byte> expectedTag = encryptData.Slice(encryptData.Length - TagLength, TagLength);

            byte[] rentedTag = ArrayPool<byte>.Shared.Rent(TagLength);
            try
            {
                Span<byte> computedTag = rentedTag.AsSpan(0, TagLength);
                ComputeTag(headerAndCipherText, computedTag, keyMaterial.AuthenticationKey);

                if (!CryptographicOperations.FixedTimeEquals(computedTag, expectedTag))
                    throw new CryptographicException("HMAC validation failed.");

                ReadOnlySpan<byte> nonce = encryptData.Slice(VersionLength, NonceLength);
                ReadOnlySpan<byte> cipherText = encryptData.Slice(VersionLength + NonceLength, plainLength);
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

        public static int GetEncryptedLength(int plainLength)
        {
            if (plainLength < 0)
                throw new ArgumentOutOfRangeException(nameof(plainLength));

            return VersionLength + NonceLength + plainLength + TagLength;
        }

        public static int GetDecryptedLength(int encryptLength)
        {
            int overhead = VersionLength + NonceLength + TagLength;
            if (encryptLength < overhead)
                throw new CryptographicException("Invalid AES-CTR payload.");

            return encryptLength - overhead;
        }

        private static void ValidatePayload(ReadOnlySpan<byte> payload)
        {
            int overhead = VersionLength + NonceLength + TagLength;
            if (payload.Length < overhead)
                throw new CryptographicException("Invalid AES-CTR payload.");

            if (payload[0] != CurrentVersion)
                throw new CryptographicException("Unsupported AES-CTR payload version.");
        }

        private static void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output, byte[] encryptionKey, ReadOnlySpan<byte> nonce)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
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
                                throw new CryptographicException("Failed to generate CTR keystream block.");

                            int take = Math.Min(BlockLength, input.Length - offset);

                            for (int i = 0; i < take; i++)
                            {
                                output[offset + i] = (byte)(input[offset + i] ^ keyStream[i]);
                            }

                            IncrementCounter(counter);
                            offset += take;
                        }

                        CryptographicOperations.ZeroMemory(counter);
                        CryptographicOperations.ZeroMemory(keyStream);
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
        }

        private static void ComputeTag(ReadOnlySpan<byte> data, Span<byte> destination, byte[] authenticationKey)
        {
            if (destination.Length < TagLength)
                throw new ArgumentException("Destination too small.", nameof(destination));

            using (HMACSHA256 hmac = new HMACSHA256(authenticationKey))
            {
                if (!hmac.TryComputeHash(data, destination, out int written) || written != TagLength)
                    throw new CryptographicException("Failed to compute HMAC-SHA256.");
            }
        }

        private static KeyMaterial GetKeyMaterial(string encryptionKey, Encoding encoding)
        {
            return KeyCache.GetOrAdd(encryptionKey, static (key, state) => DeriveKeyMaterial(key, state), encoding);
        }

        private static KeyMaterial DeriveKeyMaterial(string encryptionKey, Encoding encoding)
        {
            int byteCount = encoding.GetByteCount(encryptionKey ?? string.Empty);
            byte[] rentedKeyBytes = ArrayPool<byte>.Shared.Rent(Math.Max(1, byteCount));

            try
            {
                int written = encoding.GetBytes((encryptionKey ?? string.Empty).AsSpan(), rentedKeyBytes.AsSpan(0, byteCount));

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

                    if (encryptionSubKey.Length != DerivedKeyLength || authenticationSubKey.Length != DerivedKeyLength)
                        throw new CryptographicException("Unexpected derived key length.");

                    return new KeyMaterial(encryptionSubKey, authenticationSubKey);
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
                EncryptionKey = encryptionKey;
                AuthenticationKey = authenticationKey;
            }

            public byte[] EncryptionKey { get; }

            public byte[] AuthenticationKey { get; }
        }
    }
}