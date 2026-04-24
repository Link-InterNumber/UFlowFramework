using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public static partial class EncryptUtils
    {
        private const int AesKeyLength = 16;
        private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
        private static readonly ConcurrentDictionary<string, byte[]> AesKeyCache = new ConcurrentDictionary<string, byte[]>();

        #region DES
        
        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data">加密数据</param>
        /// <param name="key">8位字符的密钥字符串</param>
        /// <param name="iv">8位字符的初始化向量字符串</param>
        /// <returns></returns>
        public static string DESEncrypt(string data, string key, string iv)
        {
            if (key.Length < 8 || iv.Length < 8)
                throw new Exception("Key and IV must be 8 characters long");
            byte[] byKey = ASCIIEncoding.ASCII.GetBytes(key.Substring(0, 8));
            byte[] byIV = ASCIIEncoding.ASCII.GetBytes(iv.Substring(0, 8));

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            int i = cryptoProvider.KeySize;
            MemoryStream ms = new MemoryStream();
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            StreamWriter sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="data">解密数据</param>
        /// <param name="key">8位字符的密钥字符串(需要和加密时相同)</param>
        /// <param name="iv">8位字符的初始化向量字符串(需要和加密时相同)</param>
        /// <returns></returns>
        public static string DESDecrypt(string data, string key, string iv)
        {
            if (key.Length < 8 || iv.Length < 8)
                throw new Exception("Key and IV must be 8 characters long");
            byte[] byKey = ASCIIEncoding.ASCII.GetBytes(key.Substring(0, 8));
            byte[] byIV = ASCIIEncoding.ASCII.GetBytes(iv.Substring(0, 8));

            byte[] byEnc;
            try
            {
                byEnc = Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cst);
            return sr.ReadToEnd();
        }
        
        #endregion

        #region RAS

        /// <summary> 
        /// RSA加密数据 
        /// </summary> 
        /// <param name="express">要加密数据</param> 
        /// <param name="KeyContainerName">密匙容器的名称</param> 
        /// <returns></returns> 
        public static string RSAEncryption(string express, string KeyContainerName = null)
        {

            CspParameters param = new CspParameters();
            param.KeyContainerName = KeyContainerName ?? "PowerCellStudio"; //密匙容器的名称，保持加密解密一致才能解密成功
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                byte[] plaindata = Encoding.Default.GetBytes(express);//将要加密的字符串转换为字节数组
                byte[] encryptdata = rsa.Encrypt(plaindata, false);//将加密后的字节数据转换为新的加密字节数组
                return Convert.ToBase64String(encryptdata);//将加密后的字节数组转换为字符串
            }
        }
        /// <summary> 
        /// RSA解密数据 
        /// </summary> 
        /// <param name="express">要解密数据</param> 
        /// <param name="KeyContainerName">密匙容器的名称</param> 
        /// <returns></returns> 
        public static string RSADecrypt(string ciphertext, string KeyContainerName = null)
        {
            CspParameters param = new CspParameters();
            param.KeyContainerName = KeyContainerName ?? "PowerCellStudio"; //密匙容器的名称，保持加密解密一致才能解密成功
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                byte[] encryptdata = Convert.FromBase64String(ciphertext);
                byte[] decryptdata = rsa.Decrypt(encryptdata, false);
                return Encoding.Default.GetString(decryptdata);
            }
        }

        #endregion

        #region Base64加密解密
        
        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <returns></returns>
        public static string Base64Encrypt(string input)
        {
            return Base64Encrypt(input, new UTF8Encoding());
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <param name="encode">字符编码</param>
        /// <returns></returns>
        public static string Base64Encrypt(string input, Encoding encode)
        {
            return Convert.ToBase64String(encode.GetBytes(input));
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="input">需要解密的字符串</param>
        /// <returns></returns>
        public static string Base64Decrypt(string input)
        {
            return Base64Decrypt(input, new UTF8Encoding());
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="input">需要解密的字符串</param>
        /// <param name="encode">字符的编码</param>
        /// <returns></returns>
        public static string Base64Decrypt(string input, Encoding encode)
        {
            return encode.GetString(Convert.FromBase64String(input));
        }
        
        #endregion

        #region AES

        public static byte[] AESEncrypt(byte[] data, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return data;
            try
            {
                return EncryptAesBytesCore(data, encryptionKey);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return data;
            }
        }
        
        public static byte[] AESDecrypt(byte[] encryptData, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return encryptData;
            try
            {
                return DecryptAesBytesCore(encryptData, encryptionKey);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return encryptData;
            }
        }

        public static string AESEncrypt(string plainText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return plainText;
            byte[] plainBytes = Utf8Encoding.GetBytes(plainText);
            byte[] encryptedBytes = EncryptAesBytesCore(plainBytes, encryptionKey);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string AESDecrypt(string cipherText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return cipherText;
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] resultBytes = DecryptAesBytesCore(cipherBytes, encryptionKey);
            return Utf8Encoding.GetString(resultBytes, 0, resultBytes.Length);
        }

        private static byte[] EncryptAesBytesCore(byte[] data, string encryptionKey)
        {
            using (Aes aes = CreateAes(encryptionKey))
            {
                aes.GenerateIV();
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] cipherBytes = encryptor.TransformFinalBlock(data, 0, data.Length);
                    byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
                    Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
                    return result;
                }
            }
        }

        private static byte[] DecryptAesBytesCore(byte[] encryptData, string encryptionKey)
        {
            using (Aes aes = CreateAes(encryptionKey))
            {
                int ivLength = aes.BlockSize >> 3;
                if (encryptData == null || encryptData.Length < ivLength)
                    throw new CryptographicException("Invalid AES payload.");

                byte[] iv = new byte[ivLength];
                Buffer.BlockCopy(encryptData, 0, iv, 0, ivLength);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encryptData, ivLength, encryptData.Length - ivLength);
                }
            }
        }

        private static Aes CreateAes(string encryptionKey)
        {
            Aes aes = Aes.Create();
            aes.Key = GetAesKeyBytes(encryptionKey);
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private static byte[] GetAesKeyBytes(string encryptionKey)
        {
            return AesKeyCache.GetOrAdd(encryptionKey, static key =>
            {
                byte[] normalizedKey = new byte[AesKeyLength];
                byte[] sourceBytes = Utf8Encoding.GetBytes(key);
                int copyLength = Math.Min(sourceBytes.Length, AesKeyLength);
                Buffer.BlockCopy(sourceBytes, 0, normalizedKey, 0, copyLength);
                return normalizedKey;
            });
        }

        #endregion

        #region MD5
        
        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string MD5Encrypt(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 验证明文是否与MD5哈希匹配
        /// </summary>
        /// <param name="input">明文字符串</param>
        /// <param name="hash">MD5哈希字符串</param>
        /// <returns>是否匹配</returns>
        public static bool VerifyMD5(string input, string hash)
        {
            string inputHash = MD5Encrypt(input);
            return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 生成随机盐
        /// </summary>
        /// <returns>随机盐</returns>
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
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
        public static string MD5EncryptWithSalt(string input, string salt)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input + salt);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 验证明文是否与加盐的MD5哈希匹配
        /// </summary>
        /// <param name="input">明文字符串</param>
        /// <param name="salt">盐值</param>
        /// <param name="hash">MD5哈希字符串</param>
        /// <returns>是否匹配</returns>
        public static bool VerifyMD5WithSalt(string input, string salt, string hash)
        {
            string inputHash = MD5EncryptWithSalt(input, salt);
            return string.Equals(inputHash, hash, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}