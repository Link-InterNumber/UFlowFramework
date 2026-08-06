using System;
using System.Text;

namespace PowerCellStudio
{
    public static partial class EncryptUtils
    {
        private static Encoding encoding = new UTF8Encoding(false);

        // #region DES
        
        // /// <summary>
        // /// DES加密
        // /// </summary>
        // /// <param name="data">加密数据</param>
        // /// <param name="key">8位字符的密钥字符串</param>
        // /// <param name="iv">8位字符的初始化向量字符串</param>
        // /// <returns></returns>
        // public static string DESEncrypt(string data, string key, string iv)
        // {
        //     return DESEncryptor.Encrypt(data, key, iv, encoding);
        // }

        // /// <summary>
        // /// DES解密
        // /// </summary>
        // /// <param name="data">解密数据</param>
        // /// <param name="key">8位字符的密钥字符串(需要和加密时相同)</param>
        // /// <param name="iv">8位字符的初始化向量字符串(需要和加密时相同)</param>
        // /// <returns></returns>
        // public static string DESDecrypt(string data, string key, string iv)
        // {
        //     return DESEncryptor.Decrypt(data, key, iv, encoding);
        // }
        
        // #endregion

        #region RAS

        /// <summary> 
        /// RSA加密数据 
        /// </summary> 
        /// <param name="express">要加密数据</param> 
        /// <param name="KeyContainerName">密匙容器的名称</param> 
        /// <returns></returns> 
        public static string RSAEncryption(string express, string KeyContainerName = null)
        {
            return RSAEncryptor.Encrypt(express, KeyContainerName, encoding);
        }
        /// <summary> 
        /// RSA解密数据 
        /// </summary> 
        /// <param name="express">要解密数据</param> 
        /// <param name="KeyContainerName">密匙容器的名称</param> 
        /// <returns></returns> 
        public static string RSADecrypt(string ciphertext, string KeyContainerName = null)
        {
            return RSAEncryptor.Decrypt(ciphertext, KeyContainerName, encoding);
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
            return Base64Encrypt(input, encoding);
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
            return Base64Decrypt(input, encoding);
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

        public static byte[] AESEncrypt(byte[] data, string encryptionKey, int offset = 0, int count = -1)
        {
            return AesCbcEncryptor.AESEncrypt(data, encryptionKey, encoding, offset, count);
        }
        
        public static byte[] AESDecrypt(byte[] encryptData, string encryptionKey, int offset = 0, int count = -1)
        {
            return AesCbcEncryptor.AESDecrypt(encryptData, encryptionKey, encoding, offset, count);
        }

        public static string AESEncrypt(string plainText, string encryptionKey)
        {
            return AesCbcEncryptor.AESEncrypt(plainText, encryptionKey, encoding);
        }

        public static string AESDecrypt(string cipherText, string encryptionKey)
        {
            return AesCbcEncryptor.AESDecrypt(cipherText, encryptionKey, encoding);
        }

        public static byte[] AESGcmEncrypt(byte[] data, string encryptionKey)
        {
            return AesGcmEncryptor.Encrypt(data, encryptionKey, encoding);
        }
        
        public static byte[] AESGcmDecrypt(byte[] data, string encryptionKey)
        {
            return AesGcmEncryptor.Decrypt(data, encryptionKey, encoding);
        }
        
        public static string AESGcmEncrypt(string plainText, string encryptionKey)
        {
            return AesGcmEncryptor.Encrypt(plainText, encryptionKey, encoding);
        }

        public static string AESGcmDecrypt(string cipherText, string encryptionKey)
        {
            return AesGcmEncryptor.Decrypt(cipherText, encryptionKey, encoding);
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
            return MD5Encryptor.Encrypt(input, encoding);
        }

        /// <summary>
        /// 验证明文是否与MD5哈希匹配
        /// </summary>
        /// <param name="input">明文字符串</param>
        /// <param name="hash">MD5哈希字符串</param>
        /// <returns>是否匹配</returns>
        public static bool VerifyMD5(string input, string hash)
        {
            return MD5Encryptor.VerifyMD5(input, hash, encoding);
        }
        
        /// <summary>
        /// 生成随机盐
        /// </summary>
        /// <returns>随机盐</returns>
        public static string GenerateSalt()
        {
            return MD5Encryptor.GenerateSalt();
        }

        /// <summary>
        /// MD5加密（加盐）
        /// </summary>
        /// <param name="input">需要加密的字符串</param>
        /// <param name="salt">盐值</param>
        /// <returns>加密后的字符串</returns>
        public static string MD5EncryptWithSalt(string input, string salt)
        {
            return MD5Encryptor.EncryptWithSalt(input, salt, encoding);
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
            return MD5Encryptor.VerifyMD5WithSalt(input, salt, hash, encoding);
        }

        #endregion
    }
}