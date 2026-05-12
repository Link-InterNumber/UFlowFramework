using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public class RSAEncryptor
    {
        private const string DefaultKeyContainerName = "PowerCellStudio";
        private const int StackAllocThreshold = 512;

        public static string Encrypt(string plainText, string keyContainerName, Encoding encoding)
        {
            if (plainText == null)
                throw new ArgumentNullException(nameof(plainText));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            CspParameters param = new CspParameters
            {
                KeyContainerName = keyContainerName ?? DefaultKeyContainerName,
            };

            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param);

            int byteCount = encoding.GetByteCount(plainText);
            if (byteCount <= StackAllocThreshold)
            {
                Span<byte> plainBytes = stackalloc byte[byteCount];
                encoding.GetBytes(plainText.AsSpan(), plainBytes);
                byte[] encryptedBytes = rsa.Encrypt(plainBytes.ToArray(), false);
                try
                {
                    return Convert.ToBase64String(encryptedBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plainBytes);
                    CryptographicOperations.ZeroMemory(encryptedBytes);
                }
            }

            byte[] rentedPlainBytes = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                int written = encoding.GetBytes(plainText.AsSpan(), rentedPlainBytes.AsSpan(0, byteCount));
                byte[] inputBytes = new byte[written];
                rentedPlainBytes.AsSpan(0, written).CopyTo(inputBytes);
                try
                {
                    byte[] encryptedBytes = rsa.Encrypt(inputBytes, false);
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
                    CryptographicOperations.ZeroMemory(inputBytes);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedPlainBytes.AsSpan(0, byteCount));
                ArrayPool<byte>.Shared.Return(rentedPlainBytes);
            }
        }

        public static string Decrypt(string cipherText, string keyContainerName, Encoding encoding)
        {
            if (cipherText == null)
                throw new ArgumentNullException(nameof(cipherText));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            CspParameters param = new CspParameters
            {
                KeyContainerName = keyContainerName ?? DefaultKeyContainerName,
            };

            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param);

            int maxCipherByteCount = (cipherText.Length / 4) * 3 + 2;
            byte[] rentedCipherBytes = ArrayPool<byte>.Shared.Rent(maxCipherByteCount);

            try
            {
                if (!Convert.TryFromBase64String(cipherText, rentedCipherBytes, out int cipherByteCount))
                    throw new FormatException("Invalid Base64 cipher text.");

                byte[] cipherBytes = new byte[cipherByteCount];
                rentedCipherBytes.AsSpan(0, cipherByteCount).CopyTo(cipherBytes);
                try
                {
                    byte[] decryptedBytes = rsa.Decrypt(cipherBytes, false);
                    try
                    {
                        return encoding.GetString(decryptedBytes);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(decryptedBytes);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(cipherBytes);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedCipherBytes.AsSpan(0, maxCipherByteCount));
                ArrayPool<byte>.Shared.Return(rentedCipherBytes);
            }
        }
    }
}