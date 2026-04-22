using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;

public class SparseSetPerformanceTest : RunTestMono
{
    // --- 测试参数 ---
    // 增加此值以进行更重量级的测试
    private const int NUM_ITEMS = 500000; 
    // 在随机访问和移除测试中执行的操作次数
    private const int NUM_OPERATIONS = 500000;

    void Start()
    {
        UnityEngine.Debug.Log($"========== Performance Test Started (Items: {NUM_ITEMS}, Operations: {NUM_OPERATIONS}) ==========");
        UnityEngine.Debug.LogWarning("NOTE: The current SparseSet implementation has bugs that may cause crashes or incorrect results in these tests.");

        // 准备测试数据
        var itemsToAdd = new TestItem[NUM_ITEMS];
        var indicesToAccess = new int[NUM_OPERATIONS];
        var indicesToRemove = new int[NUM_ITEMS];

        var random = new System.Random();
        for (int i = 0; i < NUM_ITEMS; i++)
        {
            itemsToAdd[i] = new TestItem(i, "data_" + i);
            indicesToRemove[i] = i;
        }
        // Fisher-Yates shuffle for random removal order
        for (int i = indicesToRemove.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = indicesToRemove[i];
            indicesToRemove[i] = indicesToRemove[j];
            indicesToRemove[j] = temp;
        }
        for (int i = 0; i < NUM_OPERATIONS; i++)
        {
            // 50% chance to access an existing item, 50% non-existing
            indicesToAccess[i] = random.Next(0, NUM_ITEMS * 2);
        }

        // --- 运行测试 ---
        RunAllTests(itemsToAdd, indicesToAccess, indicesToRemove);
    }

    private static long MeasureManagedAlloc(System.Func<object> factory)
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();

        long before = System.GC.GetTotalMemory(true);
        object obj = factory();
        long after = System.GC.GetTotalMemory(true);

        // 防止被优化掉
        UnityEngine.Debug.Log(obj.GetType().Name);
        return after - before;
    }

    private void RunAllTests(TestItem[] itemsToAdd, int[] indicesToAccess, int[] indicesToRemove)
    {
        // --- SparseSet Tests ---
        UnityEngine.Debug.Log("--- Testing SparseSet<T> ---");
        var sparseSet = new SparseSet<TestItem>(NUM_ITEMS / 3, NUM_ITEMS, 1024);
        var array = new TestItem[NUM_ITEMS];
        // 测试sparseSet和array的内存大小
        long sparseSetMemory = MeasureManagedAlloc(() => new SparseSet<TestItem>(NUM_ITEMS / 3, NUM_ITEMS, 1024));
        long arrayMemory = MeasureManagedAlloc(() => new TestItem[NUM_ITEMS]);

        UnityEngine.Debug.Log(
            $"SparseSet Alloc: {sparseSetMemory / 1024f / 1024f:F2} MB, " +
            $"Array Alloc: {arrayMemory / 1024f / 1024f:F2} MB");  
        
        RunPerformanceTest("Bulk Add", () => {
            foreach (var item in itemsToAdd) sparseSet.Add(item);
        });
        RunPerformanceTest("Random Access (Contains)", () => {
            foreach (var index in indicesToAccess) sparseSet.FindOrDefault(index);
        });
        RunPerformanceTest("Iteration (foreach)", () => {
            int count = 0;
            foreach (var item in sparseSet) count++;
        });
        RunPerformanceTest("Random Remove", () => {
            foreach (var index in indicesToRemove) sparseSet.Remove(index);
        });

        // --- Dictionary Tests (For Comparison) ---
        UnityEngine.Debug.Log("--- Testing Dictionary<long, T> (Comparison) ---");
        var dictionary = new Dictionary<long, TestItem>();
        RunPerformanceTest("Bulk Add", () => {
            foreach (var item in itemsToAdd) dictionary.Add(item.index, item);
        });
        RunPerformanceTest("Random Access (ContainsKey)", () => {
            foreach (var index in indicesToAccess) dictionary.ContainsKey(index);
        });
        RunPerformanceTest("Iteration (foreach)", () => {
            int count = 0;
            foreach (var kvp in dictionary) count++;
        });
        RunPerformanceTest("Random Remove", () => {
            foreach (var index in indicesToRemove) dictionary.Remove(index);
        });

        // --- Array Tests (For Comparison) ---
        UnityEngine.Debug.Log("--- Testing TestItem[] (Comparison) ---");
        RunPerformanceTest("Bulk Add", () => {
            foreach (var item in itemsToAdd) array[item.index] = item;
        });
        RunPerformanceTest("Random Access (Bounds Check)", () => {
            foreach (var index in indicesToAccess)
            {
                if ((uint)index < (uint)array.Length)
                {
                    _ = array[index];
                }
            }
        });
        RunPerformanceTest("Iteration (foreach)", () => {
            int count = 0;
            foreach (var item in array)
            {
                if (item != null) count++;
            }
        });
        RunPerformanceTest("Random Remove", () => {
            foreach (var index in indicesToRemove) array[index] = null;
        });
    }
}