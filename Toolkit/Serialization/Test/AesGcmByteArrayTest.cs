using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// EncryptUtils.AESGcmEncrypt / AESGcmDecrypt 的 byte[] 接口测试。
    /// 挂到任意场景 GameObject 后，在 OnEnable 自动执行。
    /// </summary>
    public class AesGcmByteArrayTest : RunTestMono
    {
        // UTF-8 编码下恰好 32 字节，适用于 AES-256-GCM。
        private const string EncryptionKey = "0123456789ABCDEF0123456789ABCDEF";

        private void OnEnable()
        {
            Debug.Log("========== AES-GCM Byte[] Test Started ==========");

            TestAesCbcByteArrayRoundTrip();
            TestAesCbcByteArrayRangeRoundTrip();
            TestAesCtrHmacByteArrayRoundTrip();
            TestAesCtrHmacRejectsTamperedData();
            TestRoundTripWithNormalData();
            TestRoundTripWithEmptyData();
            TestCiphertextContainsNonceAndTag();
            TestEachEncryptionProducesDifferentCiphertext();
            TestWrongKeyCannotDecrypt();
            TestTamperedCiphertextCannotDecrypt();

            Debug.Log("========== AES-GCM Byte[] Test Finished ==========");
        }

        /// <summary>
        /// 验证 EncryptUtils 的 AES-CBC byte[] 包装接口可正确还原原始数据。
        /// </summary>
        private void TestAesCbcByteArrayRoundTrip()
        {
            RunTest("AES-CBC Byte[] roundtrip", () =>
            {
                byte[] plainData = Encoding.UTF8.GetBytes("AES-CBC byte array test. 中文内容、数字 123456。");
                byte[] encryptedData = EncryptUtils.AESEncrypt(plainData, EncryptionKey);
                byte[] decryptedData = EncryptUtils.AESDecrypt(encryptedData, EncryptionKey);

                Assert(encryptedData != null && encryptedData.Length > plainData.Length,
                    "AES-CBC ciphertext should contain an IV and padded ciphertext.");
                Assert(!plainData.SequenceEqual(encryptedData), "AES-CBC ciphertext should differ from plaintext.");
                Assert(plainData.SequenceEqual(decryptedData), "AES-CBC byte[] roundtrip failed.");
            });
        }

        /// <summary>
        /// 验证 AES-CBC byte[] 包装接口正确处理 offset 和 count。
        /// </summary>
        private void TestAesCbcByteArrayRangeRoundTrip()
        {
            RunTest("AES-CBC Byte[] range roundtrip", () =>
            {
                byte[] source = Encoding.UTF8.GetBytes("prefix|payload-to-encrypt|suffix");
                const int offset = 7;
                const int count = 18;
                byte[] expected = source.Skip(offset).Take(count).ToArray();

                byte[] encryptedData = EncryptUtils.AESEncrypt(source, EncryptionKey, offset, count);
                byte[] decryptedData = EncryptUtils.AESDecrypt(encryptedData, EncryptionKey);

                Assert(expected.SequenceEqual(decryptedData),
                    "AES-CBC should decrypt exactly the source range selected by offset and count.");
            });
        }

        /// <summary>
        /// 验证 EncryptUtils 的 AES-CTR-HMAC byte[] 包装接口可正确往返，
        /// 并生成 Version + Nonce + CipherText + HMAC 的载荷。
        /// </summary>
        private void TestAesCtrHmacByteArrayRoundTrip()
        {
            RunTest("AES-CTR-HMAC Byte[] roundtrip", () =>
            {
                byte[] plainData = Encoding.UTF8.GetBytes("AES-CTR-HMAC byte array test. 中文内容、数字 123456。");
                byte[] encryptedData = EncryptUtils.AesCtrHmacEncrypt(plainData, EncryptionKey);
                byte[] decryptedData = EncryptUtils.AesCtrHmacDecrypt(encryptedData, EncryptionKey);

                const int payloadOverhead = 1 + 16 + 32;
                Assert(encryptedData != null && encryptedData.Length == plainData.Length + payloadOverhead,
                    "AES-CTR-HMAC ciphertext should contain version, nonce, ciphertext, and HMAC tag.");
                Assert(!plainData.SequenceEqual(encryptedData), "AES-CTR-HMAC ciphertext should differ from plaintext.");
                Assert(plainData.SequenceEqual(decryptedData), "AES-CTR-HMAC byte[] roundtrip failed.");
            });
        }

        /// <summary>
        /// 验证 CTR-HMAC 的认证能力：任何载荷字节被修改后都必须拒绝解密。
        /// </summary>
        private void TestAesCtrHmacRejectsTamperedData()
        {
            RunTest("AES-CTR-HMAC rejects tampered data", () =>
            {
                byte[] plainData = Encoding.UTF8.GetBytes("Authenticated encryption must reject modified data.");
                byte[] encryptedData = EncryptUtils.AesCtrHmacEncrypt(plainData, EncryptionKey);
                byte[] tamperedData = (byte[])encryptedData.Clone();
                tamperedData[17] ^= 0x01;

                bool failedAsExpected = false;
                try
                {
                    EncryptUtils.AesCtrHmacDecrypt(tamperedData, EncryptionKey);
                }
                catch (CryptographicException)
                {
                    failedAsExpected = true;
                }

                Assert(failedAsExpected,
                    "AES-CTR-HMAC must throw CryptographicException when the authenticated payload is modified.");
            });
        }

        /// <summary>
        /// 验证：加密后能够正确还原任意普通二进制数据。
        /// </summary>
        private void TestRoundTripWithNormalData()
        {
            RunTest("AES-GCM Byte[] roundtrip", () =>
            {
                byte[] plainData = Encoding.UTF8.GetBytes(
                    "AES-GCM byte array test. 中文内容、数字 123456、符号 !@#$%^&*()。");

                byte[] encryptedData = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);
                byte[] decryptedData = EncryptUtils.AESGcmDecrypt(encryptedData, EncryptionKey);

                Assert(encryptedData != null, "Encrypted data should not be null.");
                Assert(!plainData.SequenceEqual(encryptedData),
                    "Ciphertext should not equal plaintext.");
                Assert(plainData.SequenceEqual(decryptedData),
                    "Decrypted data should equal original plaintext.");
            });
        }

        /// <summary>
        /// 验证：空数组也可被正常加解密。
        /// AES-GCM 输出仍会包含 12 字节 nonce 和 16 字节 tag。
        /// </summary>
        private void TestRoundTripWithEmptyData()
        {
            RunTest("AES-GCM empty Byte[] roundtrip", () =>
            {
                byte[] plainData = Array.Empty<byte>();

                byte[] encryptedData = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);
                byte[] decryptedData = EncryptUtils.AESGcmDecrypt(encryptedData, EncryptionKey);

                Assert(encryptedData != null, "Encrypted data should not be null.");
                Assert(encryptedData.Length == 28,
                    "Empty payload ciphertext should contain only 12-byte nonce and 16-byte tag.");
                Assert(decryptedData != null && decryptedData.Length == 0,
                    "Empty plaintext should decrypt to an empty byte array.");
            });
        }

        /// <summary>
        /// 验证输出格式：
        /// [12 bytes nonce][ciphertext][16 bytes authentication tag]
        /// 因此加密结果长度应等于原文长度 + 28。
        /// </summary>
        private void TestCiphertextContainsNonceAndTag()
        {
            RunTest("AES-GCM ciphertext format", () =>
            {
                byte[] plainData = new byte[256];
                RandomNumberGenerator.Fill(plainData);

                byte[] encryptedData = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);

                const int nonceLength = 12;
                const int tagLength = 16;
                int expectedLength = plainData.Length + nonceLength + tagLength;

                Assert(encryptedData.Length == expectedLength,
                    $"Expected encrypted length {expectedLength}, actual {encryptedData.Length}.");
            });
        }

        /// <summary>
        /// 验证随机 nonce 生效：
        /// 同一明文、同一 key 多次加密，密文应不同，
        /// 但它们都必须能还原为相同的原始数据。
        /// </summary>
        private void TestEachEncryptionProducesDifferentCiphertext()
        {
            RunTest("AES-GCM generates random nonce", () =>
            {
                byte[] plainData = Encoding.UTF8.GetBytes("Same plaintext should have different ciphertext.");

                byte[] firstEncrypted = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);
                byte[] secondEncrypted = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);

                Assert(!firstEncrypted.SequenceEqual(secondEncrypted),
                    "Two AES-GCM encryptions should differ because each uses a random nonce.");

                Assert(plainData.SequenceEqual(
                        EncryptUtils.AESGcmDecrypt(firstEncrypted, EncryptionKey)),
                    "First ciphertext could not be decrypted.");

                Assert(plainData.SequenceEqual(
                        EncryptUtils.AESGcmDecrypt(secondEncrypted, EncryptionKey)),
                    "Second ciphertext could not be decrypted.");
            });
        }

        /// <summary>
        /// 验证：使用错误 key 解密必须失败。
        /// AES-GCM 应因认证 tag 校验失败而抛出 CryptographicException。
        /// </summary>
        private void TestWrongKeyCannotDecrypt()
        {
            RunTest("AES-GCM rejects wrong key", () =>
            {
                const string wrongKey = "FEDCBA9876543210FEDCBA9876543210";
                byte[] plainData = Encoding.UTF8.GetBytes("This content must not be decrypted with a wrong key.");
                byte[] encryptedData = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);

                bool failedAsExpected = false;

                try
                {
                    EncryptUtils.AESGcmDecrypt(encryptedData, wrongKey);
                }
                catch (CryptographicException)
                {
                    failedAsExpected = true;
                }

                Assert(failedAsExpected,
                    "Decrypting AES-GCM ciphertext with a wrong key must throw CryptographicException.");
            });
        }

        /// <summary>
        /// 验证完整性保护：
        /// 篡改 nonce、密文或 tag 中任何一个字节都必须无法通过认证。
        /// </summary>
        private void TestTamperedCiphertextCannotDecrypt()
        {
            RunTest("AES-GCM detects tampered ciphertext", () =>
            {
                byte[] plainData = Encoding.UTF8.GetBytes("AES-GCM must detect data modification.");
                byte[] encryptedData = EncryptUtils.AESGcmEncrypt(plainData, EncryptionKey);

                // 复制后篡改密文区域的一个字节。
                byte[] tamperedData = (byte[])encryptedData.Clone();
                tamperedData[12] ^= 0x01;

                bool failedAsExpected = false;

                try
                {
                    EncryptUtils.AESGcmDecrypt(tamperedData, EncryptionKey);
                }
                catch (CryptographicException)
                {
                    failedAsExpected = true;
                }

                Assert(failedAsExpected,
                    "Tampered AES-GCM ciphertext must throw CryptographicException.");
            });
        }
    }
}