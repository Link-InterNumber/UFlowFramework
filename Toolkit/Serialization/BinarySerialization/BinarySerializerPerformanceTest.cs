using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

namespace PowerCellStudio
{
    public class BinarySerializerPerformanceTest : RunTestMono
    {
        private const int ItemCount = 512;
        private const int SerializeIterations = 200;
        private const int DeserializeIterations = 200;
        private const int RoundTripIterations = 100;

        private PerformancePayload _payload;
        private byte[] _binarySerializerBytes;
        private byte[] _serializeUtilsBytes;

        // 测试使用二进制方法（JSON + GZip）与 BinarySerializer 进行性能对比
        private static byte[] SerializeToBinary<T>(T data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }
            try
            {
                var op = MessagePack.Resolvers.ContractlessStandardResolver.Options;
                op = op.WithCompression(MessagePackCompression.Lz4BlockArray);
                op = op.WithCompressionMinLength(1);
                return MessagePack.MessagePackSerializer.Serialize(data, op);
                // using (var memoryStream = new MemoryStream())
                // {
                //     using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                //     using (var streamWriter = new StreamWriter(gzipStream, new UTF8Encoding(false)))
                //     using (var jsonWriter = new JsonTextWriter(streamWriter))
                //     {
                //         var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                //         {
                //             // TypeNameHandling = TypeNameHandling.Auto,
                //             PreserveReferencesHandling = PreserveReferencesHandling.Objects
                //         });
                //         serializer.Serialize(jsonWriter, data);
                //     }
                //     return memoryStream.ToArray();
                // }
                // return BinarySerializer.Serialize<T>(data);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"SerializeToBinary failed: {e.InnerException}");
                return Array.Empty<byte>();
            }
        }

        // 测试使用二进制方法（JSON + GZip）与 BinarySerializer 进行性能对比
        private static T DeserializeFromBinary<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return default;
            }
            try
            {
                var op = MessagePack.Resolvers.ContractlessStandardResolver.Options;
                op = op.WithCompression(MessagePackCompression.Lz4BlockArray);
                op = op.WithCompressionMinLength(1);
                return MessagePack.MessagePackSerializer.Deserialize<T>(bytes, op);
                // using (var compressedStream = new MemoryStream(bytes))
                // using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                // using (var streamReader = new StreamReader(gzipStream, new UTF8Encoding(false)))
                // using (var jsonReader = new JsonTextReader(streamReader))
                // {
                //     var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                //     {
                //         // TypeNameHandling = TypeNameHandling.Auto,
                //         PreserveReferencesHandling = PreserveReferencesHandling.Objects
                //     });
                //     return serializer.Deserialize<T>(jsonReader);
                // }
                // return BinarySerializer.Deserialize<T>(bytes);
            }
            catch (Exception e)
            {
                LinkLog.LogError($"DeserializeFromBinary failed: {e.InnerException}");
                return default;
            }
        }

        private void OnEnable()
        {
            Debug.Log($"========== BinarySerializer Performance Test Started (Items: {ItemCount}) ==========");

            _payload = CreatePayload(ItemCount);
            WarmUp();

            ValidateBinarySerializerRoundTrip();
            ValidateSerializeUtilsRoundTrip();
            CacheSerializedBytes();
            LogSerializedSizes();

            RunSerializeBenchmarks();
            RunDeserializeBenchmarks();
            RunRoundTripBenchmarks();

            Debug.Log("========== BinarySerializer Performance Test Finished ==========");
        }

        private void WarmUp()
        {
            BinarySerializer.Serialize(_payload);
            SerializeToBinary(_payload);
        }

        private void ValidateBinarySerializerRoundTrip()
        {
            RunTest("BinarySerializer RoundTrip Validation", () =>
            {
                PerformancePayload clone = BinarySerializer.Deserialize<PerformancePayload>(BinarySerializer.Serialize(_payload));
                AssertPayloadEquivalent(_payload, clone, "BinarySerializer");
            });
        }

        private void ValidateSerializeUtilsRoundTrip()
        {
            RunTest("SerializeUtils RoundTrip Validation", () =>
            {
                PerformancePayload clone = DeserializeFromBinary<PerformancePayload>(SerializeToBinary(_payload));
                AssertPayloadEquivalent(_payload, clone, "SerializeUtils");
            });
        }

        private void CacheSerializedBytes()
        {
            _binarySerializerBytes = BinarySerializer.Serialize(_payload);
            _serializeUtilsBytes = SerializeToBinary(_payload);
        }

        private void LogSerializedSizes()
        {
            Debug.Log($"[Size] BinarySerializer: {_binarySerializerBytes.Length} bytes");
            Debug.Log($"[Size] SerializeToBinary: {_serializeUtilsBytes.Length} bytes");
        }

        private void RunSerializeBenchmarks()
        {
            RunPerformanceTest($"BinarySerializer Serialize x{SerializeIterations}", () =>
            {
                for (int i = 0; i < SerializeIterations; i++)
                {
                    BinarySerializer.Serialize(_payload);
                }
            });

            RunPerformanceTest($"SerializeUtils SerializeToBinary x{SerializeIterations}", () =>
            {
                for (int i = 0; i < SerializeIterations; i++)
                {
                    SerializeToBinary(_payload);
                }
            });
        }

        private void RunDeserializeBenchmarks()
        {
            RunPerformanceTest($"BinarySerializer Deserialize x{DeserializeIterations}", () =>
            {
                for (int i = 0; i < DeserializeIterations; i++)
                {
                    BinarySerializer.Deserialize<PerformancePayload>(_binarySerializerBytes);
                }
            });

            RunPerformanceTest($"SerializeUtils DeserializeFromBinary x{DeserializeIterations}", () =>
            {
                for (int i = 0; i < DeserializeIterations; i++)
                {
                    DeserializeFromBinary<PerformancePayload>(_serializeUtilsBytes);
                }
            });
        }

        private void RunRoundTripBenchmarks()
        {
            RunPerformanceTest($"BinarySerializer RoundTrip x{RoundTripIterations}", () =>
            {
                for (int i = 0; i < RoundTripIterations; i++)
                {
                    byte[] bytes = BinarySerializer.Serialize(_payload);
                    BinarySerializer.Deserialize<PerformancePayload>(bytes);
                }
            });

            RunPerformanceTest($"SerializeUtils RoundTrip x{RoundTripIterations}", () =>
            {
                for (int i = 0; i < RoundTripIterations; i++)
                {
                    byte[] bytes = SerializeToBinary(_payload);
                    DeserializeFromBinary<PerformancePayload>(bytes);
                }
            });
        }

        private static PerformancePayload CreatePayload(int itemCount)
        {
            PerformancePayload payload = new PerformancePayload
            {
                Title = "binary-serializer-performance",
                Version = 3,
                CreatedAt = new DateTime(2026, 4, 20, 10, 30, 0, DateTimeKind.Utc),
                Duration = TimeSpan.FromMinutes(42),
                Items = new List<PerformanceItem>(itemCount),
                Lookup = new Dictionary<string, int>(itemCount),
                Tags = new HashSet<string>(),
                Sections = new List<PerformanceSection>()
            };

            for (int i = 0; i < itemCount; i++)
            {
                PerformanceItem item = new PerformanceItem
                {
                    Id = i,
                    Name = "Item_" + i,
                    Weight = i * 0.75f,
                    Score = i * 1.25d,
                    Token = Guid.NewGuid(),
                    Delay = TimeSpan.FromMilliseconds(i * 5),
                    Samples = new List<int> { i, i + 1, i + 2, i + 3 },
                    Stats = new PerformanceStats
                    {
                        Health = 100 + i,
                        Mana = 50 + (i % 10),
                        Active = (i % 2) == 0
                    }
                };

                payload.Items.Add(item);
                payload.Lookup["key_" + i] = i * 3;
                payload.Tags.Add("tag_" + (i % 16));
            }

            for (int i = 0; i < 12; i++)
            {
                payload.Sections.Add(new PerformanceSection
                {
                    Name = "section_" + i,
                    Indices = new List<int> { i, i + 10, i + 20, i + 30 }
                });
            }

            return payload;
        }

        private void AssertPayloadEquivalent(PerformancePayload expected, PerformancePayload actual, string sourceName)
        {
            Assert(actual != null, sourceName + " clone should not be null.");
            Assert(actual.Title == expected.Title, sourceName + " title mismatch.");
            Assert(actual.Version == expected.Version, sourceName + " version mismatch.");
            Assert(actual.CreatedAt == expected.CreatedAt, sourceName + " created time mismatch.");
            Assert(actual.Duration == expected.Duration, sourceName + " duration mismatch.");
            Assert(actual.Items != null && actual.Items.Count == expected.Items.Count, sourceName + " item count mismatch.");
            Assert(actual.Lookup != null && actual.Lookup.Count == expected.Lookup.Count, sourceName + " lookup count mismatch.");
            Assert(actual.Tags != null && actual.Tags.SetEquals(expected.Tags), sourceName + " tags mismatch.");
            Assert(actual.Sections != null && actual.Sections.Count == expected.Sections.Count, sourceName + " section count mismatch.");

            PerformanceItem expectedFirst = expected.Items[0];
            PerformanceItem actualFirst = actual.Items[0];
            Assert(actualFirst.Id == expectedFirst.Id, sourceName + " first item id mismatch.");
            Assert(actualFirst.Name == expectedFirst.Name, sourceName + " first item name mismatch.");
            Assert(Math.Abs(actualFirst.Weight - expectedFirst.Weight) < 0.0001f, sourceName + " first item weight mismatch.");
            Assert(Math.Abs(actualFirst.Score - expectedFirst.Score) < 0.0001d, sourceName + " first item score mismatch.");
            Assert(actualFirst.Token == expectedFirst.Token, sourceName + " first item token mismatch.");
            Assert(actualFirst.Delay == expectedFirst.Delay, sourceName + " first item delay mismatch.");
            Assert(actualFirst.Samples.SequenceEqual(expectedFirst.Samples), sourceName + " first item samples mismatch.");
            Assert(actualFirst.Stats.Health == expectedFirst.Stats.Health, sourceName + " first item stats health mismatch.");
            Assert(actualFirst.Stats.Mana == expectedFirst.Stats.Mana, sourceName + " first item stats mana mismatch.");
            Assert(actualFirst.Stats.Active == expectedFirst.Stats.Active, sourceName + " first item stats active mismatch.");
            Assert(actual.Lookup["key_10"] == expected.Lookup["key_10"], sourceName + " lookup value mismatch.");
            Assert(actual.Sections[0].Indices.SequenceEqual(expected.Sections[0].Indices), sourceName + " section indices mismatch.");
        }

        [Serializable]
        public class PerformancePayload
        {
            public string Title;
            public int Version;
            public DateTime CreatedAt;
            public TimeSpan Duration;
            public List<PerformanceItem> Items;
            public Dictionary<string, int> Lookup;
            public HashSet<string> Tags;
            public List<PerformanceSection> Sections;
        }

        [Serializable]
        public class PerformanceItem
        {
            public int Id;
            public string Name;
            public float Weight;
            public double Score;
            public Guid Token;
            public TimeSpan Delay;
            public List<int> Samples;
            public PerformanceStats Stats;
        }

        [Serializable]
        public class PerformanceStats
        {
            public int Health;
            public int Mana;
            public bool Active;
        }

        [Serializable]
        public class PerformanceSection
        {
            public string Name;
            public List<int> Indices;
        }
    }
}