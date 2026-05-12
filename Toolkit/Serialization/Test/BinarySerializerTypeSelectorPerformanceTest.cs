using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PowerCellStudio
{
    public class BinarySerializerTypeSelectorPerformanceTest : RunTestMono
    {
        private const int ItemCount = 4096;
        private const int SerializeIterations = 256;
        private const int DeserializeIterations = 256;
        private const int RoundTripIterations = 128;

        private static bool s_selectorRegistered;

        private SelectorValue[] _selectorValues;
    private BinaryDataValue[] _binaryDataValues;
        private PlainValue[] _plainValues;
        private byte[] _selectorBytes;
    private byte[] _binaryDataBytes;
        private byte[] _plainBytes;

        private void OnEnable()
        {
            EnsureSelectorRegistered();

            Debug.Log($"========== BinarySerializer TypeSelector Performance Test Started (Items: {ItemCount}) ==========");

            BuildPayload();
            WarmUp();
            ValidateRoundTrip();
            CacheSerializedBytes();
            LogSerializedSizes();

            RunSerializeBenchmarks();
            RunDeserializeBenchmarks();
            RunRoundTripBenchmarks();

            Debug.Log("========== BinarySerializer TypeSelector Performance Test Finished ==========");
        }

        private static void EnsureSelectorRegistered()
        {
            if (s_selectorRegistered)
            {
                return;
            }

            BinarySerializer.RegisterCustomSelector(new SelectorValueTypeSelector());
            s_selectorRegistered = true;
        }

        private void BuildPayload()
        {
            _selectorValues = new SelectorValue[ItemCount];
            _binaryDataValues = new BinaryDataValue[ItemCount];
            _plainValues = new PlainValue[ItemCount];

            for (int i = 0; i < ItemCount; i++)
            {
                long ticks = 638700000000000000L + (i * 97L);
                int level = i % 33;
                float weight = i * 0.125f;
                double score = i * 1.61803398875d;
                bool enabled = (i & 1) == 0;

                _selectorValues[i] = new SelectorValue
                {
                    Ticks = ticks,
                    Level = level,
                    Weight = weight,
                    Score = score,
                    Enabled = enabled
                };

                _binaryDataValues[i] = new BinaryDataValue
                {
                    Ticks = ticks,
                    Level = level,
                    Weight = weight,
                    Score = score,
                    Enabled = enabled
                };

                _plainValues[i] = new PlainValue
                {
                    Ticks = ticks,
                    Level = level,
                    Weight = weight,
                    Score = score,
                    Enabled = enabled
                };
            }
        }

        private void WarmUp()
        {
            BinarySerializer.Serialize(_selectorValues);
            BinarySerializer.Serialize(_binaryDataValues);
            BinarySerializer.Serialize(_plainValues);
            BinarySerializer.Deserialize<SelectorValue[]>(BinarySerializer.Serialize(_selectorValues));
            BinarySerializer.Deserialize<BinaryDataValue[]>(BinarySerializer.Serialize(_binaryDataValues));
            BinarySerializer.Deserialize<PlainValue[]>(BinarySerializer.Serialize(_plainValues));
        }

        private void ValidateRoundTrip()
        {
            RunTest("Custom Serializer RoundTrip Validation", () =>
            {
                SelectorValue[] selectorClone = BinarySerializer.Deserialize<SelectorValue[]>(BinarySerializer.Serialize(_selectorValues));
                BinaryDataValue[] binaryDataClone = BinarySerializer.Deserialize<BinaryDataValue[]>(BinarySerializer.Serialize(_binaryDataValues));
                PlainValue[] plainClone = BinarySerializer.Deserialize<PlainValue[]>(BinarySerializer.Serialize(_plainValues));

                Assert(selectorClone != null, "Selector clone should not be null.");
                Assert(binaryDataClone != null, "IBinaryData clone should not be null.");
                Assert(plainClone != null, "Plain clone should not be null.");
                Assert(selectorClone.Length == _selectorValues.Length, "Selector clone length mismatch.");
                Assert(binaryDataClone.Length == _binaryDataValues.Length, "IBinaryData clone length mismatch.");
                Assert(plainClone.Length == _plainValues.Length, "Plain clone length mismatch.");

                AssertSelectorValue(selectorClone[0], _selectorValues[0], "Selector first item mismatch.");
                AssertSelectorValue(selectorClone[selectorClone.Length - 1], _selectorValues[_selectorValues.Length - 1], "Selector last item mismatch.");
                AssertBinaryDataValue(binaryDataClone[0], _binaryDataValues[0], "IBinaryData first item mismatch.");
                AssertBinaryDataValue(binaryDataClone[binaryDataClone.Length - 1], _binaryDataValues[_binaryDataValues.Length - 1], "IBinaryData last item mismatch.");
                AssertPlainValue(plainClone[0], _plainValues[0], "Plain first item mismatch.");
                AssertPlainValue(plainClone[plainClone.Length - 1], _plainValues[_plainValues.Length - 1], "Plain last item mismatch.");
            });
        }

        private void CacheSerializedBytes()
        {
            _selectorBytes = BinarySerializer.Serialize(_selectorValues);
            _binaryDataBytes = BinarySerializer.Serialize(_binaryDataValues);
            _plainBytes = BinarySerializer.Serialize(_plainValues);
        }

        private void LogSerializedSizes()
        {
            RunTest("Custom Serializer Serialized Size Validation", () =>
            {
                Assert(_selectorBytes.Length == _plainBytes.Length, "Selector and plain payload size should match.");
                Assert(_binaryDataBytes.Length == _plainBytes.Length, "IBinaryData and plain payload size should match.");
            });

            Debug.Log($"[Size] With selector: {_selectorBytes.Length} bytes");
            Debug.Log($"[Size] With IBinaryData: {_binaryDataBytes.Length} bytes");
            Debug.Log($"[Size] Without selector: {_plainBytes.Length} bytes");
        }

        private void RunSerializeBenchmarks()
        {
            RunPerformanceTest($"BinarySerializer Serialize with selector x{SerializeIterations}", () =>
            {
                for (int i = 0; i < SerializeIterations; i++)
                {
                    BinarySerializer.Serialize(_selectorValues);
                }
            });

            RunPerformanceTest($"BinarySerializer Serialize with IBinaryData x{SerializeIterations}", () =>
            {
                for (int i = 0; i < SerializeIterations; i++)
                {
                    BinarySerializer.Serialize(_binaryDataValues);
                }
            });

            RunPerformanceTest($"BinarySerializer Serialize without selector x{SerializeIterations}", () =>
            {
                for (int i = 0; i < SerializeIterations; i++)
                {
                    BinarySerializer.Serialize(_plainValues);
                }
            });

            LogComparison(
                $"Serialize x{SerializeIterations}",
                () =>
                {
                    for (int i = 0; i < SerializeIterations; i++)
                    {
                        BinarySerializer.Serialize(_selectorValues);
                    }
                },
                () =>
                {
                    for (int i = 0; i < SerializeIterations; i++)
                    {
                        BinarySerializer.Serialize(_plainValues);
                    }
                });

            LogComparison(
                $"Serialize IBinaryData vs plain x{SerializeIterations}",
                () =>
                {
                    for (int i = 0; i < SerializeIterations; i++)
                    {
                        BinarySerializer.Serialize(_binaryDataValues);
                    }
                },
                () =>
                {
                    for (int i = 0; i < SerializeIterations; i++)
                    {
                        BinarySerializer.Serialize(_plainValues);
                    }
                });

            LogComparison(
                $"Serialize selector vs IBinaryData x{SerializeIterations}",
                () =>
                {
                    for (int i = 0; i < SerializeIterations; i++)
                    {
                        BinarySerializer.Serialize(_selectorValues);
                    }
                },
                () =>
                {
                    for (int i = 0; i < SerializeIterations; i++)
                    {
                        BinarySerializer.Serialize(_binaryDataValues);
                    }
                });
        }

        private void RunDeserializeBenchmarks()
        {
            RunPerformanceTest($"BinarySerializer Deserialize with selector x{DeserializeIterations}", () =>
            {
                for (int i = 0; i < DeserializeIterations; i++)
                {
                    BinarySerializer.Deserialize<SelectorValue[]>(_selectorBytes);
                }
            });

            RunPerformanceTest($"BinarySerializer Deserialize with IBinaryData x{DeserializeIterations}", () =>
            {
                for (int i = 0; i < DeserializeIterations; i++)
                {
                    BinarySerializer.Deserialize<BinaryDataValue[]>(_binaryDataBytes);
                }
            });

            RunPerformanceTest($"BinarySerializer Deserialize without selector x{DeserializeIterations}", () =>
            {
                for (int i = 0; i < DeserializeIterations; i++)
                {
                    BinarySerializer.Deserialize<PlainValue[]>(_plainBytes);
                }
            });

            LogComparison(
                $"Deserialize x{DeserializeIterations}",
                () =>
                {
                    for (int i = 0; i < DeserializeIterations; i++)
                    {
                        BinarySerializer.Deserialize<SelectorValue[]>(_selectorBytes);
                    }
                },
                () =>
                {
                    for (int i = 0; i < DeserializeIterations; i++)
                    {
                        BinarySerializer.Deserialize<PlainValue[]>(_plainBytes);
                    }
                });

            LogComparison(
                $"Deserialize IBinaryData vs plain x{DeserializeIterations}",
                () =>
                {
                    for (int i = 0; i < DeserializeIterations; i++)
                    {
                        BinarySerializer.Deserialize<BinaryDataValue[]>(_binaryDataBytes);
                    }
                },
                () =>
                {
                    for (int i = 0; i < DeserializeIterations; i++)
                    {
                        BinarySerializer.Deserialize<PlainValue[]>(_plainBytes);
                    }
                });

            LogComparison(
                $"Deserialize selector vs IBinaryData x{DeserializeIterations}",
                () =>
                {
                    for (int i = 0; i < DeserializeIterations; i++)
                    {
                        BinarySerializer.Deserialize<SelectorValue[]>(_selectorBytes);
                    }
                },
                () =>
                {
                    for (int i = 0; i < DeserializeIterations; i++)
                    {
                        BinarySerializer.Deserialize<BinaryDataValue[]>(_binaryDataBytes);
                    }
                });
        }

        private void RunRoundTripBenchmarks()
        {
            RunPerformanceTest($"BinarySerializer RoundTrip with selector x{RoundTripIterations}", () =>
            {
                for (int i = 0; i < RoundTripIterations; i++)
                {
                    byte[] bytes = BinarySerializer.Serialize(_selectorValues);
                    BinarySerializer.Deserialize<SelectorValue[]>(bytes);
                }
            });

            RunPerformanceTest($"BinarySerializer RoundTrip with IBinaryData x{RoundTripIterations}", () =>
            {
                for (int i = 0; i < RoundTripIterations; i++)
                {
                    byte[] bytes = BinarySerializer.Serialize(_binaryDataValues);
                    BinarySerializer.Deserialize<BinaryDataValue[]>(bytes);
                }
            });

            RunPerformanceTest($"BinarySerializer RoundTrip without selector x{RoundTripIterations}", () =>
            {
                for (int i = 0; i < RoundTripIterations; i++)
                {
                    byte[] bytes = BinarySerializer.Serialize(_plainValues);
                    BinarySerializer.Deserialize<PlainValue[]>(bytes);
                }
            });

            LogComparison(
                $"RoundTrip x{RoundTripIterations}",
                () =>
                {
                    for (int i = 0; i < RoundTripIterations; i++)
                    {
                        byte[] bytes = BinarySerializer.Serialize(_selectorValues);
                        BinarySerializer.Deserialize<SelectorValue[]>(bytes);
                    }
                },
                () =>
                {
                    for (int i = 0; i < RoundTripIterations; i++)
                    {
                        byte[] bytes = BinarySerializer.Serialize(_plainValues);
                        BinarySerializer.Deserialize<PlainValue[]>(bytes);
                    }
                });

            LogComparison(
                $"RoundTrip IBinaryData vs plain x{RoundTripIterations}",
                () =>
                {
                    for (int i = 0; i < RoundTripIterations; i++)
                    {
                        byte[] bytes = BinarySerializer.Serialize(_binaryDataValues);
                        BinarySerializer.Deserialize<BinaryDataValue[]>(bytes);
                    }
                },
                () =>
                {
                    for (int i = 0; i < RoundTripIterations; i++)
                    {
                        byte[] bytes = BinarySerializer.Serialize(_plainValues);
                        BinarySerializer.Deserialize<PlainValue[]>(bytes);
                    }
                });

            LogComparison(
                $"RoundTrip selector vs IBinaryData x{RoundTripIterations}",
                () =>
                {
                    for (int i = 0; i < RoundTripIterations; i++)
                    {
                        byte[] bytes = BinarySerializer.Serialize(_selectorValues);
                        BinarySerializer.Deserialize<SelectorValue[]>(bytes);
                    }
                },
                () =>
                {
                    for (int i = 0; i < RoundTripIterations; i++)
                    {
                        byte[] bytes = BinarySerializer.Serialize(_binaryDataValues);
                        BinarySerializer.Deserialize<BinaryDataValue[]>(bytes);
                    }
                });
        }

        private static void AssertSelectorValue(SelectorValue actual, SelectorValue expected, string message)
        {
            if (actual.Ticks != expected.Ticks ||
                actual.Level != expected.Level ||
                Math.Abs(actual.Weight - expected.Weight) > 0.0001f ||
                Math.Abs(actual.Score - expected.Score) > 0.0000001d ||
                actual.Enabled != expected.Enabled)
            {
                throw new Exception(message);
            }
        }

        private static void AssertPlainValue(PlainValue actual, PlainValue expected, string message)
        {
            if (actual.Ticks != expected.Ticks ||
                actual.Level != expected.Level ||
                Math.Abs(actual.Weight - expected.Weight) > 0.0001f ||
                Math.Abs(actual.Score - expected.Score) > 0.0000001d ||
                actual.Enabled != expected.Enabled)
            {
                throw new Exception(message);
            }
        }

        private static void AssertBinaryDataValue(BinaryDataValue actual, BinaryDataValue expected, string message)
        {
            if (actual.Ticks != expected.Ticks ||
                actual.Level != expected.Level ||
                Math.Abs(actual.Weight - expected.Weight) > 0.0001f ||
                Math.Abs(actual.Score - expected.Score) > 0.0000001d ||
                actual.Enabled != expected.Enabled)
            {
                throw new Exception(message);
            }
        }

        private static void LogComparison(string phaseName, Action selectorAction, Action plainAction)
        {
            double selectorMs = MeasureMilliseconds(selectorAction);
            double plainMs = MeasureMilliseconds(plainAction);
            double deltaMs = plainMs - selectorMs;
            double improvement = plainMs <= 0d ? 0d : (deltaMs / plainMs) * 100d;

            Debug.Log(
                $"[Compare] {phaseName}: with selector = {selectorMs:F2} ms, without selector = {plainMs:F2} ms, delta = {deltaMs:F2} ms, improvement = {improvement:F2}%");
        }

        private static double MeasureMilliseconds(Action action)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        [Serializable]
        public struct SelectorValue
        {
            public long Ticks;
            public int Level;
            public float Weight;
            public double Score;
            public bool Enabled;
        }

        [Serializable]
        public struct PlainValue
        {
            public long Ticks;
            public int Level;
            public float Weight;
            public double Score;
            public bool Enabled;
        }

        [Serializable]
        public struct BinaryDataValue : IBinaryData
        {
            public long Ticks;
            public int Level;
            public float Weight;
            public double Score;
            public bool Enabled;

            public void WriteData(BinaryWriter writer, Encoding encoding)
            {
                writer.Write(Ticks);
                writer.Write(Level);
                writer.Write(Weight);
                writer.Write(Score);
                writer.Write(Enabled);
            }

            public void ReadData(BinaryReader reader, Encoding encoding)
            {
                Ticks = reader.ReadInt64();
                Level = reader.ReadInt32();
                Weight = reader.ReadSingle();
                Score = reader.ReadDouble();
                Enabled = reader.ReadBoolean();
            }
        }

        private sealed class SelectorValueTypeSelector : BinarySerializerTypeSelector<SelectorValue>
        {
            public override void Write(BinaryWriter writer, SelectorValue value, Encoding encoding)
            {
                writer.Write(value.Ticks);
                writer.Write(value.Level);
                writer.Write(value.Weight);
                writer.Write(value.Score);
                writer.Write(value.Enabled);
            }

            public override SelectorValue Read(BinaryReader reader, Encoding encoding)
            {
                return new SelectorValue
                {
                    Ticks = reader.ReadInt64(),
                    Level = reader.ReadInt32(),
                    Weight = reader.ReadSingle(),
                    Score = reader.ReadDouble(),
                    Enabled = reader.ReadBoolean()
                };
            }
        }
    }
}