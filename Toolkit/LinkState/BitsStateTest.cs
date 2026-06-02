using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using PowerCellStudio;

public class BitsStateTest : RunTestMono
{
    [Header("Data Size")]
    [SerializeField] private int stateCapacity = 65536;
    [SerializeField] private int operationCount = 500000;
    [SerializeField] private int matchGroupCount = 100000;

    [Header("Run Options")]
    [SerializeField] private int randomSeed = 20260601;

    private int[] operationIndices;
    private int[] matchIndices;

    private void OnEnable()
    {

        RunAll();
    }

    [ContextMenu("Run BitsState Tests")]
    public void RunAll()
    {
        ValidateConfig();
        PrepareData();

        UnityEngine.Debug.Log($"========== BitsState Test Suite Started (capacity={stateCapacity}, ops={operationCount}, matchGroups={matchGroupCount}) ==========");

        RunCorrectnessTests();
        RunPerformanceComparison();
        RunMemoryComparison();

        UnityEngine.Debug.Log("========== BitsState Test Suite Finished ==========");
    }

    private void ValidateConfig()
    {
        stateCapacity = Math.Max(128, stateCapacity);
        operationCount = Math.Max(1000, operationCount);
        matchGroupCount = Math.Max(1000, matchGroupCount);
    }

    private void PrepareData()
    {
        var random = new System.Random(randomSeed);

        operationIndices = new int[operationCount];
        for (int i = 0; i < operationCount; i++)
        {
            operationIndices[i] = random.Next(0, stateCapacity);
        }

        // Every 3 indices form one match group.
        matchIndices = new int[matchGroupCount * 3];
        for (int i = 0; i < matchIndices.Length; i++)
        {
            matchIndices[i] = random.Next(0, stateCapacity);
        }
    }

    private void RunCorrectnessTests()
    {
        RunTest("BitsState Set/Get/Clear", () =>
        {
            var bitsState = new BitsState();

            bitsState.SetState(0, true);
            bitsState.SetState(63, true);
            bitsState.SetState(64, true);
            bitsState.SetState(130, true);

            Assert(bitsState.GetState(0), "State 0 should be true.");
            Assert(bitsState.GetState(63), "State 63 should be true.");
            Assert(bitsState.GetState(64), "State 64 should be true.");
            Assert(bitsState.GetState(130), "State 130 should be true.");

            bitsState.SetState(64, false);
            Assert(!bitsState.GetState(64), "State 64 should be false after reset.");

            bitsState.Clear();
            Assert(!bitsState.GetState(0), "State 0 should be false after Clear.");
            Assert(!bitsState.GetState(130), "State 130 should be false after Clear.");
        });

        RunTest("BitsState SetBatchState/IsMatch", () =>
        {
            var bitsState = new BitsState();
            bitsState.SetBatchState(true, 2, 5, 7, 127, 511);

            Assert(bitsState.IsMatch(2, 5, 7), "2/5/7 should all match.");
            Assert(bitsState.IsMatch(127, 511), "127/511 should both match.");
            Assert(!bitsState.IsMatch(2, 3), "2/3 should not match because 3 is false.");

            bitsState.SetBatchState(false, 5, 511);
            Assert(!bitsState.IsMatch(2, 5, 7), "2/5/7 should fail after 5 reset.");
            Assert(!bitsState.IsMatch(127, 511), "127/511 should fail after 511 reset.");
        });
    }

    private void RunPerformanceComparison()
    {
        RunPerformanceTest($"BitsState SetState(true) x{operationCount}", () =>
        {
            var bitsState = new BitsState();
            for (int i = 0; i < operationIndices.Length; i++)
            {
                bitsState.SetState(operationIndices[i], true);
            }
        });

        RunPerformanceTest($"bool[] set true x{operationCount}", () =>
        {
            var states = new bool[stateCapacity];
            for (int i = 0; i < operationIndices.Length; i++)
            {
                states[operationIndices[i]] = true;
            }
        });

        RunPerformanceTest($"HashSet<int> add x{operationCount}", () =>
        {
            var states = new HashSet<int>();
            for (int i = 0; i < operationIndices.Length; i++)
            {
                states.Add(operationIndices[i]);
            }
        });

        RunPerformanceTest($"List<bool> set true x{operationCount}", () =>
        {
            var states = CreateFalseBoolList(stateCapacity);
            for (int i = 0; i < operationIndices.Length; i++)
            {
                states[operationIndices[i]] = true;
            }
        });

        RunPerformanceTest($"BitsState GetState x{operationCount}", () =>
        {
            var bitsState = BuildFilledBitsState();
            int hitCount = 0;
            for (int i = 0; i < operationIndices.Length; i++)
            {
                if (bitsState.GetState(operationIndices[i]))
                {
                    hitCount++;
                }
            }

            if (hitCount < 0)
            {
                UnityEngine.Debug.Log(hitCount);
            }
        });

        RunPerformanceTest($"bool[] get x{operationCount}", () =>
        {
            var states = BuildFilledBoolState();
            int hitCount = 0;
            for (int i = 0; i < operationIndices.Length; i++)
            {
                if (states[operationIndices[i]])
                {
                    hitCount++;
                }
            }

            if (hitCount < 0)
            {
                UnityEngine.Debug.Log(hitCount);
            }
        });

        RunPerformanceTest($"HashSet<int> Contains x{operationCount}", () =>
        {
            var states = BuildFilledHashSetState();
            int hitCount = 0;
            for (int i = 0; i < operationIndices.Length; i++)
            {
                if (states.Contains(operationIndices[i]))
                {
                    hitCount++;
                }
            }

            if (hitCount < 0)
            {
                UnityEngine.Debug.Log(hitCount);
            }
        });

        RunPerformanceTest($"List<bool> get x{operationCount}", () =>
        {
            var states = BuildFilledListBoolState();
            int hitCount = 0;
            for (int i = 0; i < operationIndices.Length; i++)
            {
                if (states[operationIndices[i]])
                {
                    hitCount++;
                }
            }

            if (hitCount < 0)
            {
                UnityEngine.Debug.Log(hitCount);
            }
        });

        RunPerformanceTest($"BitsState IsMatch(3 states) x{matchGroupCount}", () =>
        {
            var bitsState = BuildFilledBitsState();
            int passCount = 0;
            for (int i = 0; i < matchIndices.Length; i += 3)
            {
                if (bitsState.IsMatch(matchIndices[i], matchIndices[i + 1], matchIndices[i + 2]))
                {
                    passCount++;
                }
            }

            if (passCount < 0)
            {
                UnityEngine.Debug.Log(passCount);
            }
        });

        RunPerformanceTest($"bool[] IsMatch(3 states) x{matchGroupCount}", () =>
        {
            var states = BuildFilledBoolState();
            int passCount = 0;
            for (int i = 0; i < matchIndices.Length; i += 3)
            {
                if (states[matchIndices[i]] && states[matchIndices[i + 1]] && states[matchIndices[i + 2]])
                {
                    passCount++;
                }
            }

            if (passCount < 0)
            {
                UnityEngine.Debug.Log(passCount);
            }
        });

        RunPerformanceTest($"HashSet<int> IsMatch(3 states) x{matchGroupCount}", () =>
        {
            var states = BuildFilledHashSetState();
            int passCount = 0;
            for (int i = 0; i < matchIndices.Length; i += 3)
            {
                if (states.Contains(matchIndices[i]) && states.Contains(matchIndices[i + 1]) && states.Contains(matchIndices[i + 2]))
                {
                    passCount++;
                }
            }

            if (passCount < 0)
            {
                UnityEngine.Debug.Log(passCount);
            }
        });

        RunPerformanceTest($"List<bool> IsMatch(3 states) x{matchGroupCount}", () =>
        {
            var states = BuildFilledListBoolState();
            int passCount = 0;
            for (int i = 0; i < matchIndices.Length; i += 3)
            {
                if (states[matchIndices[i]] && states[matchIndices[i + 1]] && states[matchIndices[i + 2]])
                {
                    passCount++;
                }
            }

            if (passCount < 0)
            {
                UnityEngine.Debug.Log(passCount);
            }
        });

        LogSpeedup("SetState vs bool[] set", "bool[]", MeasureBitsSet, MeasureBoolSet);
        LogSpeedup("SetState vs HashSet<int> add", "HashSet<int>", MeasureBitsSet, MeasureHashSetSet);
        LogSpeedup("SetState vs List<bool> set", "List<bool>", MeasureBitsSet, MeasureListBoolSet);
        LogSpeedup("GetState vs bool[] get", "bool[]", MeasureBitsGet, MeasureBoolGet);
        LogSpeedup("GetState vs HashSet<int> Contains", "HashSet<int>", MeasureBitsGet, MeasureHashSetGet);
        LogSpeedup("GetState vs List<bool> get", "List<bool>", MeasureBitsGet, MeasureListBoolGet);
        LogSpeedup("IsMatch(3) vs bool[] logic", "bool[]", MeasureBitsMatch, MeasureBoolMatch);
        LogSpeedup("IsMatch(3) vs HashSet<int> logic", "HashSet<int>", MeasureBitsMatch, MeasureHashSetMatch);
        LogSpeedup("IsMatch(3) vs List<bool> logic", "List<bool>", MeasureBitsMatch, MeasureListBoolMatch);
    }

    private void RunMemoryComparison()
    {
        LogMemorySizeComparison();

        RunMemoryTest($"BitsState memory (set 0..{stateCapacity - 1})", () =>
        {
            var bitsState = new BitsState();
            for (int i = 0; i < stateCapacity; i++)
            {
                bitsState.SetState(i, true);
            }
        });

        RunMemoryTest($"bool[] memory ({stateCapacity})", () =>
        {
            var states = new bool[stateCapacity];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = true;
            }
        });

        RunMemoryTest($"HashSet<int> memory (add 0..{stateCapacity - 1})", () =>
        {
            var states = new HashSet<int>();
            for (int i = 0; i < stateCapacity; i++)
            {
                states.Add(i);
            }
        });

        RunMemoryTest($"List<bool> memory ({stateCapacity})", () =>
        {
            var states = CreateFalseBoolList(stateCapacity);
            for (int i = 0; i < states.Count; i++)
            {
                states[i] = true;
            }
        });
    }

    private void LogMemorySizeComparison()
    {
        var bitsState = new BitsState();
        for (int i = 0; i < stateCapacity; i++)
        {
            bitsState.SetState(i, true);
        }

        var boolStates = new bool[stateCapacity];
        for (int i = 0; i < boolStates.Length; i++)
        {
            boolStates[i] = true;
        }

        var hashStates = new HashSet<int>();
        for (int i = 0; i < stateCapacity; i++)
        {
            hashStates.Add(i);
        }

        var listBoolStates = CreateFalseBoolList(stateCapacity);
        for (int i = 0; i < listBoolStates.Count; i++)
        {
            listBoolStates[i] = true;
        }

        long bitsStorage = EstimateBitsStateStorageBytes(bitsState);
        long boolStorage = boolStates.LongLength * sizeof(bool);
        long hashStorage = EstimateHashSetStorageBytes(hashStates);
        long listBoolStorage = (long)listBoolStates.Capacity * sizeof(bool);

        UnityEngine.Debug.Log(
            $"[MEM-SIZE][STORAGE] BitsState={FormatBytes(bitsStorage)}, bool[]={FormatBytes(boolStorage)}, HashSet<int>={FormatBytes(hashStorage)}, List<bool>={FormatBytes(listBoolStorage)}");

        long boolRetained = MeasureRetainedManagedMemory(() =>
        {
            var item = new bool[stateCapacity];
            for (int i = 0; i < item.Length; i++)
            {
                item[i] = true;
            }

            return item;
        });
        
        long bitsRetained = MeasureRetainedManagedMemory(() =>
        {
            var item = new BitsState();
            for (int i = 0; i < stateCapacity; i++)
            {
                item.SetState(i, true);
            }

            return item;
        });


        long hashRetained = MeasureRetainedManagedMemory(() =>
        {
            var item = new HashSet<int>();
            for (int i = 0; i < stateCapacity; i++)
            {
                item.Add(i);
            }

            return item;
        });

        long listBoolRetained = MeasureRetainedManagedMemory(() =>
        {
            var item = CreateFalseBoolList(stateCapacity);
            for (int i = 0; i < item.Count; i++)
            {
                item[i] = true;
            }

            return item;
        });

        UnityEngine.Debug.Log(
            $"[MEM-SIZE][MANAGED] BitsState={FormatBytes(bitsRetained)}, bool[]={FormatBytes(boolRetained)}, HashSet<int>={FormatBytes(hashRetained)}, List<bool>={FormatBytes(listBoolRetained)}");
    }

    private BitsState BuildFilledBitsState()
    {
        var bitsState = new BitsState();
        for (int i = 0; i < operationIndices.Length; i++)
        {
            bitsState.SetState(operationIndices[i], true);
        }

        return bitsState;
    }

    private bool[] BuildFilledBoolState()
    {
        var states = new bool[stateCapacity];
        for (int i = 0; i < operationIndices.Length; i++)
        {
            states[operationIndices[i]] = true;
        }

        return states;
    }

    private HashSet<int> BuildFilledHashSetState()
    {
        var states = new HashSet<int>();
        for (int i = 0; i < operationIndices.Length; i++)
        {
            states.Add(operationIndices[i]);
        }

        return states;
    }

    private List<bool> BuildFilledListBoolState()
    {
        var states = CreateFalseBoolList(stateCapacity);
        for (int i = 0; i < operationIndices.Length; i++)
        {
            states[operationIndices[i]] = true;
        }

        return states;
    }

    private static List<bool> CreateFalseBoolList(int capacity)
    {
        var states = new List<bool>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            states.Add(false);
        }

        return states;
    }

    private void LogSpeedup(string title, string baselineLabel, Action bitsAction, Action baselineAction)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var bitsMs = MeasureMs(bitsAction);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var baselineMs = MeasureMs(baselineAction);

        var ratio = bitsMs > 0.0 ? baselineMs / bitsMs : double.PositiveInfinity;
        var fasterText = ratio >= 1.0
            ? $"BitsState faster x{ratio:F2}"
            : $"{baselineLabel} faster x{(1.0 / ratio):F2}";

        UnityEngine.Debug.Log($"[COMPARE] {title}: BitsState={bitsMs:F2} ms, {baselineLabel}={baselineMs:F2} ms, {fasterText}");
    }

    private double MeasureMs(Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private void MeasureBitsSet()
    {
        var bitsState = new BitsState();
        for (int i = 0; i < operationIndices.Length; i++)
        {
            bitsState.SetState(operationIndices[i], true);
        }
    }

    private void MeasureBoolSet()
    {
        var states = new bool[stateCapacity];
        for (int i = 0; i < operationIndices.Length; i++)
        {
            states[operationIndices[i]] = true;
        }
    }

    private void MeasureHashSetSet()
    {
        var states = new HashSet<int>();
        for (int i = 0; i < operationIndices.Length; i++)
        {
            states.Add(operationIndices[i]);
        }
    }

    private void MeasureListBoolSet()
    {
        var states = CreateFalseBoolList(stateCapacity);
        for (int i = 0; i < operationIndices.Length; i++)
        {
            states[operationIndices[i]] = true;
        }
    }

    private void MeasureBitsGet()
    {
        var bitsState = BuildFilledBitsState();
        int hitCount = 0;
        for (int i = 0; i < operationIndices.Length; i++)
        {
            if (bitsState.GetState(operationIndices[i]))
            {
                hitCount++;
            }
        }

        if (hitCount < 0)
        {
            UnityEngine.Debug.Log(hitCount);
        }
    }

    private void MeasureBoolGet()
    {
        var states = BuildFilledBoolState();
        int hitCount = 0;
        for (int i = 0; i < operationIndices.Length; i++)
        {
            if (states[operationIndices[i]])
            {
                hitCount++;
            }
        }

        if (hitCount < 0)
        {
            UnityEngine.Debug.Log(hitCount);
        }
    }

    private void MeasureHashSetGet()
    {
        var states = BuildFilledHashSetState();
        int hitCount = 0;
        for (int i = 0; i < operationIndices.Length; i++)
        {
            if (states.Contains(operationIndices[i]))
            {
                hitCount++;
            }
        }

        if (hitCount < 0)
        {
            UnityEngine.Debug.Log(hitCount);
        }
    }

    private void MeasureListBoolGet()
    {
        var states = BuildFilledListBoolState();
        int hitCount = 0;
        for (int i = 0; i < operationIndices.Length; i++)
        {
            if (states[operationIndices[i]])
            {
                hitCount++;
            }
        }

        if (hitCount < 0)
        {
            UnityEngine.Debug.Log(hitCount);
        }
    }

    private void MeasureBitsMatch()
    {
        var bitsState = BuildFilledBitsState();
        int passCount = 0;
        for (int i = 0; i < matchIndices.Length; i += 3)
        {
            if (bitsState.IsMatch(matchIndices[i], matchIndices[i + 1], matchIndices[i + 2]))
            {
                passCount++;
            }
        }

        if (passCount < 0)
        {
            UnityEngine.Debug.Log(passCount);
        }
    }

    private void MeasureBoolMatch()
    {
        var states = BuildFilledBoolState();
        int passCount = 0;
        for (int i = 0; i < matchIndices.Length; i += 3)
        {
            if (states[matchIndices[i]] && states[matchIndices[i + 1]] && states[matchIndices[i + 2]])
            {
                passCount++;
            }
        }

        if (passCount < 0)
        {
            UnityEngine.Debug.Log(passCount);
        }
    }

    private void MeasureHashSetMatch()
    {
        var states = BuildFilledHashSetState();
        int passCount = 0;
        for (int i = 0; i < matchIndices.Length; i += 3)
        {
            if (states.Contains(matchIndices[i]) && states.Contains(matchIndices[i + 1]) && states.Contains(matchIndices[i + 2]))
            {
                passCount++;
            }
        }

        if (passCount < 0)
        {
            UnityEngine.Debug.Log(passCount);
        }
    }

    private void MeasureListBoolMatch()
    {
        var states = BuildFilledListBoolState();
        int passCount = 0;
        for (int i = 0; i < matchIndices.Length; i += 3)
        {
            if (states[matchIndices[i]] && states[matchIndices[i + 1]] && states[matchIndices[i + 2]])
            {
                passCount++;
            }
        }

        if (passCount < 0)
        {
            UnityEngine.Debug.Log(passCount);
        }
    }

    private static long EstimateBitsStateStorageBytes(BitsState bitsState)
    {
        var bitsField = typeof(BitsState).GetField("bits", BindingFlags.NonPublic | BindingFlags.Instance);
        if (bitsField == null)
        {
            return -1;
        }

        object listObj = bitsField.GetValue(bitsState);
        if (listObj is List<bool> boolList)
        {
            return (long)boolList.Capacity * sizeof(bool);
        }

        if (listObj is List<ulong> ulongList)
        {
            return (long)ulongList.Capacity * sizeof(ulong);
        }

        return -1;
    }

    private static long EstimateHashSetStorageBytes(HashSet<int> states)
    {
        var type = typeof(HashSet<int>);

        var bucketsField = type.GetField("_buckets", BindingFlags.NonPublic | BindingFlags.Instance) ??
                           type.GetField("m_buckets", BindingFlags.NonPublic | BindingFlags.Instance);

        var entriesField = type.GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance) ??
                           type.GetField("_slots", BindingFlags.NonPublic | BindingFlags.Instance) ??
                           type.GetField("m_slots", BindingFlags.NonPublic | BindingFlags.Instance);

        if (bucketsField == null || entriesField == null)
        {
            return -1;
        }

        var buckets = bucketsField.GetValue(states) as Array;
        var entries = entriesField.GetValue(states) as Array;
        if (buckets == null || entries == null)
        {
            return -1;
        }

        // For int key: hashCode(int) + next(int) + value(int).
        const int entryBytes = sizeof(int) * 3;
        return (long)buckets.Length * sizeof(int) + (long)entries.Length * entryBytes;
    }

    private static long MeasureRetainedManagedMemory(Func<object> factory)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetTotalMemory(true);
        object instance = factory();
        long after = GC.GetTotalMemory(true);

        GC.KeepAlive(instance);
        return after - before;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "N/A";
        }

        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024f:F2} KB";
        }

        return $"{bytes / 1024f / 1024f:F2} MB";
    }
}
