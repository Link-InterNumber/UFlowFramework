
using System;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public class AesGcmEncryptor
    {
        public static string Encrypt(string plainText, string encryptionKey, Encoding encoding)
        {
            Span<byte> plainBytes = stackalloc byte[encoding.GetByteCount(plainText)];
            encoding.GetBytes(plainText, plainBytes);
            using var aesGcm = new AesGcm(encoding.GetBytes(encryptionKey));
            Span<byte> nonce = stackalloc byte[12];
            RandomNumberGenerator.Fill(nonce);
            Span<byte> tag = stackalloc byte[16];
            Span<byte> cipherText = stackalloc byte[plainBytes.Length];
            aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
            Span<byte> encryptedData = stackalloc byte[nonce.Length + cipherText.Length + tag.Length];
            nonce.CopyTo(encryptedData);
            cipherText.CopyTo(encryptedData.Slice(nonce.Length));
            tag.CopyTo(encryptedData.Slice(nonce.Length + cipherText.Length));
            return Convert.ToBase64String(encryptedData);
        }

        public static string Decrypt(string cipherText, string encryptionKey, Encoding encoding)
        {
            var cipherBytes = Convert.FromBase64String(cipherText).AsSpan();
            using var aesGcm = new AesGcm(encoding.GetBytes(encryptionKey));
            var nonce = cipherBytes.Slice(0, 12); // 前12字节为Nonce
            var tag = cipherBytes.Slice(cipherBytes.Length - 16); // 后16字节为Tag
            var cipher = cipherBytes.Slice(12, cipherBytes.Length - 28); // 中间部分为密文
            Span<byte> decryptedData = stackalloc byte[cipher.Length];
            aesGcm.Decrypt(nonce, cipher, tag, decryptedData);
            return encoding.GetString(decryptedData);
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> encryptData, string encryptionKey, Encoding encoding)
        {
            using var aesGcm = new AesGcm(encoding.GetBytes(encryptionKey));
            var nonce = encryptData.Slice(0, 12); // 前12字节为Nonce
            var tag = encryptData.Slice(encryptData.Length - 16); // 后16字节为Tag
            var cipherText = encryptData.Slice(12, encryptData.Length - 28); // 中间部分为密文
            var decryptedData = new byte[cipherText.Length];
            aesGcm.Decrypt(nonce, cipherText, tag, decryptedData);
            return decryptedData;
        }

        public static Span<byte> DecryptAsSpan(ReadOnlySpan<byte> encryptData, string encryptionKey, Encoding encoding)
        {
            return Decrypt(encryptData, encryptionKey, encoding);
        }

        public static Span<byte> EncryptAsSpan(ReadOnlySpan<byte> plainData, string encryptionKey, Encoding encoding)
        {
            using var aesGcm = new AesGcm(encoding.GetBytes(encryptionKey));
            Span<byte> nonce = stackalloc byte[12];
            RandomNumberGenerator.Fill(nonce);
            Span<byte> tag = stackalloc byte[16];
            Span<byte> cipherText = stackalloc byte[plainData.Length];
            aesGcm.Encrypt(nonce, plainData, cipherText, tag);
            Span<byte> encryptedData = new byte[nonce.Length + cipherText.Length + tag.Length];
            nonce.CopyTo(encryptedData);
            cipherText.CopyTo(encryptedData.Slice(nonce.Length));
            tag.CopyTo(encryptedData.Slice(nonce.Length + cipherText.Length));
            return encryptedData;
        }

        public static byte[] Encrypt(ReadOnlySpan<byte> plainData, string encryptionKey, Encoding encoding)
        {
            return EncryptAsSpan(plainData, encryptionKey, encoding).ToArray();
        }
    }
}