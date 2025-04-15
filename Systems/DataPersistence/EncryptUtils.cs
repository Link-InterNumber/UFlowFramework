using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PowerCellStudio
{
    public static partial class EncryptUtils
    {
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
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
                Array.Resize(ref keyBytes, 16);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.GenerateIV();
                    aes.Padding = PaddingMode.PKCS7;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        return ms.ToArray();
                    }
                }
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
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
                Array.Resize(ref keyBytes, 16);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.Padding = PaddingMode.PKCS7;
                    using (MemoryStream ms = new MemoryStream(encryptData))
                    {
                        byte[] iv = new byte[aes.IV.Length];
                        ms.Read(iv, 0, iv.Length);
                        aes.IV = iv;
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            byte[] plainBytes = new byte[encryptData.Length - iv.Length];
                            int decryptedCount = cs.Read(plainBytes, 0, plainBytes.Length);
                            Array.Resize(ref plainBytes, decryptedCount);
                            return plainBytes;
                        }
                    }
                }
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
            byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
            Array.Resize(ref keyBytes, 16);
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.GenerateIV();
                aes.Padding = PaddingMode.PKCS7; // Ensure the same padding mode is used
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string AESDecrypt(string cipherText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey)) return cipherText;
            byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);
            Array.Resize(ref keyBytes, 16);
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Padding = PaddingMode.PKCS7; // Ensure the same padding mode is used
                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    byte[] iv = new byte[aes.IV.Length];
                    ms.Read(iv, 0, iv.Length);
                    aes.IV = iv;
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        byte[] plainBytes = new byte[cipherBytes.Length - iv.Length];
                        int decryptedCount = cs.Read(plainBytes, 0, plainBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes, 0, decryptedCount);
                    }
                }
            }
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