using System;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public class MD5Encryptor
    {
        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string Encrypt(string input, Encoding encoding)
        {
            using (MD5 md5 = MD5.Create())
            {
                Span<byte> plainBytes = stackalloc byte[encoding.GetByteCount(input)];
                encoding.GetBytes(input, plainBytes);
                Span<byte> hashBytesSpan = stackalloc byte[md5.HashSize / 8];
                if (md5.TryComputeHash(plainBytes, hashBytesSpan, out int bytesWritten))
                {
                    StringBuilder sb = new StringBuilder(bytesWritten * 2);
                    for (int i = 0; i < bytesWritten; i++)
                    {
                        sb.Append(hashBytesSpan[i].ToString("X2"));
                    }

                    return sb.ToString();
                }
                else
                {
                    byte[] inputBytes = encoding.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }

                    return sb.ToString();
                }
            }
        }

        /// <summary>
        /// 验证明文是否与MD5哈希匹配
        /// </summary>
        /// <param name="input">明文字符串</param>
        /// <param name="hash">MD5哈希字符串</param>
        /// <returns>是否匹配</returns>
        public static bool VerifyMD5(string input, string hash, Encoding encoding)
        {
            string inputHash = Encrypt(input, encoding);
            return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 生成随机盐
        /// </summary>
        /// <returns>随机盐</returns>
        public static string GenerateSalt()
        {
            Span<byte> saltBytes = stackalloc byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// MD5加密（加盐）
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <param name="salt">盐值</param>
        /// <returns>加密后的字符串</returns>
        public static string EncryptWithSalt(string input, string salt, Encoding encoding)
        {
            using (MD5 md5 = MD5.Create())
            {
                Span<char> inputWithSalt = stackalloc char[input.Length + salt.Length];
                input.AsSpan().CopyTo(inputWithSalt);
                salt.AsSpan().CopyTo(inputWithSalt.Slice(input.Length));
                Span<byte> inputBytes = stackalloc byte[encoding.GetByteCount(inputWithSalt)];
                encoding.GetBytes(inputWithSalt, inputBytes);
                Span<byte> hashBytesSpan = stackalloc byte[md5.HashSize / 8];
                if (md5.TryComputeHash(inputBytes, hashBytesSpan, out int bytesWritten))
                {
                    StringBuilder sb = new StringBuilder(bytesWritten * 2);
                    for (int i = 0; i < bytesWritten; i++)
                    {
                        sb.Append(hashBytesSpan[i].ToString("X2"));
                    }

                    return sb.ToString();
                }
                else
                {
                    byte[] inputBytesArray = encoding.GetBytes(input + salt);
                    byte[] hashBytes = md5.ComputeHash(inputBytesArray);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }

                    return sb.ToString();
                }
            }
        }

        /// <summary>
        /// 验证明文是否与加盐的MD5哈希匹配
        /// </summary>
        /// <param name="input">明文字符串</param>
        /// <param name="salt">盐值</param>
        /// <param name="hash">MD5哈希字符串</param>
        /// <returns>是否匹配</returns>
        public static bool VerifyMD5WithSalt(string input, string salt, string hash, Encoding encoding)
        {
            string inputHash = EncryptWithSalt(input, salt, encoding);
            return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}