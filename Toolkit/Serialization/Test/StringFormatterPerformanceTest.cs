using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PowerCellStudio
{
    public class StringFormatterPerformanceTest : RunTestMono
    {
        private static readonly TestCase[] Cases =
        {
            new TestCase("short-ascii", "serialization-ok", 200000),
            new TestCase("medium-ascii", new string('a', 128), 120000),
            new TestCase("large-ascii", new string('b', 2048), 30000),
            new TestCase("unicode", string.Concat(Enumerable.Repeat("测试🙂", 64)), 60000),
            new TestCase("empty", string.Empty, 200000),
            new TestCase("null", null, 200000),
        };

        private readonly StringFormatter _formatter = new StringFormatter();

        private void OnEnable()
        {
            Debug.Log("========== StringFormatter Performance Test Started ==========");

            WarmUp();
            ValidateCompatibility();
            RunBenchmarks();

            Debug.Log("========== StringFormatter Performance Test Finished ==========");
        }

        private void WarmUp()
        {
            foreach (var item in Cases)
            {
                CurrentWrite(item.Value);
                OldWrite(item.Value);
                CurrentRead(CurrentWrite(item.Value));
                OldRead(OldWrite(item.Value));
            }
        }

        private void ValidateCompatibility()
        {
            foreach (var item in Cases)
            {
                string caseName = item.Name;
                RunTest($"StringFormatter compatibility {caseName}", () =>
                {
                    byte[] currentBytes = CurrentWrite(item.Value);
                    byte[] oldBytes = OldWrite(item.Value);

                    Assert(currentBytes.SequenceEqual(oldBytes), $"Serialized bytes mismatch for {caseName}.");
                    Assert(CurrentRead(currentBytes) == item.Value, $"Current read mismatch for {caseName}.");
                    Assert(OldRead(oldBytes) == item.Value, $"Old read mismatch for {caseName}.");
                });
            }
        }

        private void RunBenchmarks()
        {
            foreach (var item in Cases)
            {
                string caseName = item.Name;
                byte[] currentBytes = CurrentWrite(item.Value);
                byte[] oldBytes = OldWrite(item.Value);
                
                RunProfilerTest($"StringFormatter current write memory {caseName} x{item.Iterations}", () =>
                {
                    for (int i = 0; i < item.Iterations; i++)
                    {
                        CurrentWrite(item.Value);
                    }
                });

                RunProfilerTest($"StringFormatter old write memory {caseName} x{item.Iterations}", () =>
                {
                    for (int i = 0; i < item.Iterations; i++)
                    {
                        OldWrite(item.Value);
                    }
                });

                // RunPerformanceTest($"StringFormatter current write {caseName} x{item.Iterations}", () =>
                // {
                //     for (int i = 0; i < item.Iterations; i++)
                //     {
                //         CurrentWrite(item.Value);
                //     }
                // });

                // RunPerformanceTest($"StringFormatter old write {caseName} x{item.Iterations}", () =>
                // {
                //     for (int i = 0; i < item.Iterations; i++)
                //     {
                //         OldWrite(item.Value);
                //     }
                // });
                

                // LogComparison(
                //     $"Write {caseName} x{item.Iterations}",
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             CurrentWrite(item.Value);
                //         }
                //     },
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             OldWrite(item.Value);
                //         }
                //     });

                // LogMemoryComparison(
                //     $"Write memory {caseName} x{item.Iterations}",
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             CurrentWrite(item.Value);
                //         }
                //     },
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             OldWrite(item.Value);
                //         }
                //     });

                // RunPerformanceTest($"StringFormatter current read {caseName} x{item.Iterations}", () =>
                // {
                //     for (int i = 0; i < item.Iterations; i++)
                //     {
                //         CurrentRead(currentBytes);
                //     }
                // });

                // RunPerformanceTest($"StringFormatter old read {caseName} x{item.Iterations}", () =>
                // {
                //     for (int i = 0; i < item.Iterations; i++)
                //     {
                //         OldRead(oldBytes);
                //     }
                // });

                // RunMemoryTest($"StringFormatter current read memory {caseName} x{item.Iterations}", () =>
                // {
                //     for (int i = 0; i < item.Iterations; i++)
                //     {
                //         CurrentRead(currentBytes);
                //     }
                // });

                // RunMemoryTest($"StringFormatter old read memory {caseName} x{item.Iterations}", () =>
                // {
                //     for (int i = 0; i < item.Iterations; i++)
                //     {
                //         OldRead(oldBytes);
                //     }
                // });

                // LogComparison(
                //     $"Read {caseName} x{item.Iterations}",
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             CurrentRead(currentBytes);
                //         }
                //     },
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             OldRead(oldBytes);
                //         }
                //     });

                // LogMemoryComparison(
                //     $"Read memory {caseName} x{item.Iterations}",
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             CurrentRead(currentBytes);
                //         }
                //     },
                //     () =>
                //     {
                //         for (int i = 0; i < item.Iterations; i++)
                //         {
                //             OldRead(oldBytes);
                //         }
                //     });

                // int roundTripIterations = Math.Max(1, item.Iterations / 4);

                // RunPerformanceTest($"StringFormatter current roundtrip {caseName} x{roundTripIterations}", () =>
                // {
                //     for (int i = 0; i < roundTripIterations; i++)
                //     {
                //         CurrentRead(CurrentWrite(item.Value));
                //     }
                // });

                // RunPerformanceTest($"StringFormatter old roundtrip {caseName} x{roundTripIterations}", () =>
                // {
                //     for (int i = 0; i < roundTripIterations; i++)
                //     {
                //         OldRead(OldWrite(item.Value));
                //     }
                // });

                // RunMemoryTest($"StringFormatter current roundtrip memory {caseName} x{roundTripIterations}", () =>
                // {
                //     for (int i = 0; i < roundTripIterations; i++)
                //     {
                //         CurrentRead(CurrentWrite(item.Value));
                //     }
                // });

                // RunMemoryTest($"StringFormatter old roundtrip memory {caseName} x{roundTripIterations}", () =>
                // {
                //     for (int i = 0; i < roundTripIterations; i++)
                //     {
                //         OldRead(OldWrite(item.Value));
                //     }
                // });

                // LogComparison(
                //     $"RoundTrip {caseName} x{roundTripIterations}",
                //     () =>
                //     {
                //         for (int i = 0; i < roundTripIterations; i++)
                //         {
                //             CurrentRead(CurrentWrite(item.Value));
                //         }
                //     },
                //     () =>
                //     {
                //         for (int i = 0; i < roundTripIterations; i++)
                //         {
                //             OldRead(OldWrite(item.Value));
                //         }
                //     });

                // LogMemoryComparison(
                //     $"RoundTrip memory {caseName} x{roundTripIterations}",
                //     () =>
                //     {
                //         for (int i = 0; i < roundTripIterations; i++)
                //         {
                //             CurrentRead(CurrentWrite(item.Value));
                //         }
                //     },
                //     () =>
                //     {
                //         for (int i = 0; i < roundTripIterations; i++)
                //         {
                //             OldRead(OldWrite(item.Value));
                //         }
                //     });
            }
        }

        private byte[] CurrentWrite(string value)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, BinarySerializer.Encoding))
            {
                _formatter.Write(writer, value, BinarySerializer.Encoding);
                return ms.ToArray();
            }
        }

        private string CurrentRead(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms, BinarySerializer.Encoding))
            {
                return _formatter.Read(reader, BinarySerializer.Encoding);
            }
        }

        private static byte[] OldWrite(string value)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, BinarySerializer.Encoding))
            {
                byte[] bytes = BinarySerializer.Encoding.GetBytes(value ?? string.Empty);
                writer.Write(value == null ? -1 : bytes.Length);
                if (bytes.Length > 0)
                {
                    writer.Write(bytes);
                }

                return ms.ToArray();
            }
        }

        private static string OldRead(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms, BinarySerializer.Encoding))
            {
                int length = reader.ReadInt32();
                if (length < 0)
                    return null;
                if (length == 0)
                    return string.Empty;

                byte[] raw = reader.ReadBytes(length);
                return BinarySerializer.Encoding.GetString(raw);
            }
        }

        private static void LogComparison(string phaseName, Action currentAction, Action oldAction)
        {
            double currentMs = MeasureMilliseconds(currentAction);
            double oldMs = MeasureMilliseconds(oldAction);
            double deltaMs = oldMs - currentMs;
            double improvement = oldMs <= 0d ? 0d : (deltaMs / oldMs) * 100d;

            Debug.Log(
                $"[Compare] {phaseName}: current = {currentMs:F2} ms, old = {oldMs:F2} ms, delta = {deltaMs:F2} ms, improvement = {improvement:F2}%");
        }

        private static void LogMemoryComparison(string phaseName, Action currentAction, Action oldAction)
        {
            long currentBytes = MeasureManagedMemoryDelta(currentAction);
            long oldBytes = MeasureManagedMemoryDelta(oldAction);
            long deltaBytes = oldBytes - currentBytes;
            double improvement = oldBytes <= 0L ? 0d : (deltaBytes / (double)oldBytes) * 100d;

            Debug.Log(
                $"[Memory] {phaseName}: current = {currentBytes} bytes, old = {oldBytes} bytes, delta = {deltaBytes} bytes, improvement = {improvement:F2}%");
        }

        private static double MeasureMilliseconds(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        private static long MeasureManagedMemoryDelta(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetTotalMemory(true);
            action();
            long after = GC.GetTotalMemory(false);
            return after - before;
        }

        private readonly struct TestCase
        {
            public readonly string Name;
            public readonly string Value;
            public readonly int Iterations;

            public TestCase(string name, string value, int iterations)
            {
                Name = name;
                Value = value;
                Iterations = iterations;
            }
        }
    }
}