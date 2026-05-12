using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public class DESEncryptor
    {
        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data">加密数据</param>
        /// <param name="key">8位字符的密钥字符串</param>
        /// <param name="iv">8位字符的初始化向量字符串</param>
        /// <returns></returns>
        public static string Encrypt(string data, string key, string iv, Encoding encoding)
        {
            if (key.Length < 8 || iv.Length < 8)
                throw new Exception("Key and IV must be 8 characters long");
            var keySpan = key.AsSpan().Slice(0, 8);
            var ivSpan = iv.AsSpan().Slice(0, 8);
            
            var byKey = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(keySpan));
            encoding.GetBytes(keySpan, byKey);

            var byIV = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(ivSpan));
            encoding.GetBytes(ivSpan, byIV);

            using DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            int i = cryptoProvider.KeySize;
            using MemoryStream ms = new MemoryStream();
            using CryptoStream cst =
                new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            using StreamWriter sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            ArrayPool<byte>.Shared.Return(byKey);
            ArrayPool<byte>.Shared.Return(byIV);
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="data">解密数据</param>
        /// <param name="key">8位字符的密钥字符串(需要和加密时相同)</param>
        /// <param name="iv">8位字符的初始化向量字符串(需要和加密时相同)</param>
        /// <returns></returns>
        public static string Decrypt(string data, string key, string iv, Encoding encoding)
        {
            if (key.Length < 8 || iv.Length < 8)
                throw new Exception("Key and IV must be 8 characters long");
            var keySpan = key.AsSpan().Slice(0, 8);
            var ivSpan = iv.AsSpan().Slice(0, 8);

            var byKey = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(keySpan));
            encoding.GetBytes(keySpan, byKey);

            var byIV = ArrayPool<byte>.Shared.Rent(encoding.GetByteCount(ivSpan));
            encoding.GetBytes(ivSpan, byIV);

            byte[] byEnc;
            try
            {
                byEnc = Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byKey);
                ArrayPool<byte>.Shared.Return(byIV);
            }

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cst);
            string result = sr.ReadToEnd();
            ArrayPool<byte>.Shared.Return(byKey);
            ArrayPool<byte>.Shared.Return(byIV);
            return result;
        }
    }
}