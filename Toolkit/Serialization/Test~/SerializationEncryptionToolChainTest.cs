using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public class SerializationEncryptionToolChainTest : RunTestMono
    {
        private const string TestEncryptionKey = "SerializeKey@2026";

        private void OnEnable()
        {
            Debug.Log("========== Serialization Encryption ToolChain Test Suite Started ==========");

            TestJsonSerializeRoundTrip();
            TestBinarySerializeRoundTrip();
            TestAesStringRoundTrip();
            TestAesBytesRoundTrip();
            TestJsonAesToolChainRoundTrip();
            TestBinaryAesToolChainRoundTrip();
            TestAsyncBinarySerializeRoundTrip();

            Debug.Log("========== Serialization Encryption ToolChain Test Suite Finished ==========");
        }

        private void TestJsonSerializeRoundTrip()
        {
            RunTest("SerializeUtils JSON RoundTrip", () =>
            {
                TestPayload source = CreatePayload();

                string json = SerializeUtils.SerializeToJson(source);
                TestPayload clone = SerializeUtils.DeserializeFromJson<TestPayload>(json);

                Assert(!string.IsNullOrEmpty(json), "Serialized json should not be empty.");
                AssertPayloadEqual(source, clone, "JSON roundtrip");
            });
        }

        private void TestBinarySerializeRoundTrip()
        {
            RunTest("SerializeUtils Binary RoundTrip", () =>
            {
                TestPayload source = CreatePayload();

                byte[] bytes = SerializeUtils.SerializeToBinary(source);
                TestPayload clone = SerializeUtils.DeserializeFromBinary<TestPayload>(bytes);

                Assert(bytes != null && bytes.Length > 0, "Serialized binary should not be empty.");
                AssertPayloadEqual(source, clone, "Binary roundtrip");
            });
        }

        private void TestAesStringRoundTrip()
        {
            RunTest("EncryptUtils AES String RoundTrip", () =>
            {
                string plainText = SerializeUtils.SerializeToJson(CreatePayload(), true);
                string encryptedText = EncryptUtils.AESEncrypt(plainText, TestEncryptionKey);
                string decryptedText = EncryptUtils.AESDecrypt(encryptedText, TestEncryptionKey);

                Assert(!string.Equals(plainText, encryptedText, StringComparison.Ordinal), "Encrypted string should differ from plain text.");
                Assert(string.Equals(plainText, decryptedText, StringComparison.Ordinal), "AES string roundtrip failed.");
            });
        }

        private void TestAesBytesRoundTrip()
        {
            RunTest("EncryptUtils AES Bytes RoundTrip", () =>
            {
                byte[] plainBytes = SerializeUtils.SerializeToBinary(CreatePayload());
                byte[] encryptedBytes = EncryptUtils.AESEncrypt(plainBytes, TestEncryptionKey);
                byte[] decryptedBytes = EncryptUtils.AESDecrypt(encryptedBytes, TestEncryptionKey);

                Assert(encryptedBytes != null && encryptedBytes.Length > plainBytes.Length, "Encrypted bytes should include IV and payload.");
                Assert(plainBytes.SequenceEqual(decryptedBytes), "AES byte roundtrip failed.");
            });
        }

        private void TestJsonAesToolChainRoundTrip()
        {
            RunTest("JSON + AES ToolChain RoundTrip", () =>
            {
                TestPayload source = CreatePayload();

                string json = SerializeUtils.SerializeToJson(source);
                string encryptedText = EncryptUtils.AESEncrypt(json, TestEncryptionKey);
                string decryptedJson = EncryptUtils.AESDecrypt(encryptedText, TestEncryptionKey);
                TestPayload clone = SerializeUtils.DeserializeFromJson<TestPayload>(decryptedJson);

                AssertPayloadEqual(source, clone, "JSON + AES toolchain");
            });
        }

        private void TestBinaryAesToolChainRoundTrip()
        {
            RunTest("Binary + AES ToolChain RoundTrip", () =>
            {
                TestPayload source = CreatePayload();

                byte[] bytes = SerializeUtils.SerializeToBinary(source);
                byte[] encryptedBytes = EncryptUtils.AESEncrypt(bytes, TestEncryptionKey);
                byte[] decryptedBytes = EncryptUtils.AESDecrypt(encryptedBytes, TestEncryptionKey);
                TestPayload clone = SerializeUtils.DeserializeFromBinary<TestPayload>(decryptedBytes);

                AssertPayloadEqual(source, clone, "Binary + AES toolchain");
            });
        }

        private void TestAsyncBinarySerializeRoundTrip()
        {
            RunTest("SerializeUtils Async Binary RoundTrip", () =>
            {
                TestPayload source = CreatePayload();

                byte[] bytes = SerializeUtils.SerializeToBinaryAsync(source).GetAwaiter().GetResult();
                TestPayload clone = SerializeUtils.DeserializeFromBinaryAsync<TestPayload>(bytes).GetAwaiter().GetResult();

                Assert(bytes != null && bytes.Length > 0, "Async serialized bytes should not be empty.");
                AssertPayloadEqual(source, clone, "Async binary roundtrip");
            });
        }

        private static TestPayload CreatePayload()
        {
            return new TestPayload
            {
                Id = 1024,
                Name = "toolchain-payload",
                Enabled = true,
                Score = 98.75f,
                Tags = new List<string> { "save", "encrypt", "binary" },
                Metadata = new Dictionary<string, int>
                {
                    { "hp", 135 },
                    { "mp", 42 }
                },
                Child = new ChildPayload
                {
                    Code = "nested-node",
                    Level = 7
                }
            };
        }

        private void AssertPayloadEqual(TestPayload expected, TestPayload actual, string scenario)
        {
            Assert(actual != null, $"{scenario} should deserialize payload.");
            Assert(actual.Id == expected.Id, $"{scenario} id mismatch.");
            Assert(actual.Name == expected.Name, $"{scenario} name mismatch.");
            Assert(actual.Enabled == expected.Enabled, $"{scenario} enabled mismatch.");
            Assert(Math.Abs(actual.Score - expected.Score) < 0.0001f, $"{scenario} score mismatch.");
            Assert(actual.Tags != null && actual.Tags.SequenceEqual(expected.Tags), $"{scenario} tags mismatch.");
            Assert(actual.Metadata != null, $"{scenario} metadata should not be null.");
            Assert(actual.Metadata.Count == expected.Metadata.Count, $"{scenario} metadata count mismatch.");
            Assert(actual.Metadata["hp"] == expected.Metadata["hp"], $"{scenario} hp mismatch.");
            Assert(actual.Metadata["mp"] == expected.Metadata["mp"], $"{scenario} mp mismatch.");
            Assert(actual.Child != null, $"{scenario} child should not be null.");
            Assert(actual.Child.Code == expected.Child.Code, $"{scenario} child code mismatch.");
            Assert(actual.Child.Level == expected.Child.Level, $"{scenario} child level mismatch.");
        }

        [Serializable]
        private class TestPayload
        {
            public int Id;
            public string Name;
            public bool Enabled;
            public float Score;
            public List<string> Tags;
            public Dictionary<string, int> Metadata;
            public ChildPayload Child;
        }

        [Serializable]
        private class ChildPayload
        {
            public string Code;
            public int Level;
        }
    }
}