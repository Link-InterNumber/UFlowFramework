using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Diagnostics;

public class OrderListPerformanceTest : RunTestMono
{
    // --- 测试参数 ---
    // 增加此值以进行更重量级的测试
    private const int NUM_ITEMS = 100000;
    // 在随机访问和移除测试中执行的操作次数
    private const int NUM_OPERATIONS = 50000;

    void Start()
    {
        UnityEngine.Debug.Log($"========== OrderList Performance Test Started (Items: {NUM_ITEMS}, Operations: {NUM_OPERATIONS}) ==========");

        // 准备测试数据
        var itemsToAdd = new int[NUM_ITEMS];
        var indicesToAccess = new int[NUM_OPERATIONS];
        var itemsToRemove = new int[NUM_ITEMS];

        var random = new System.Random();
        for (int i = 0; i < NUM_ITEMS; i++)
        {
            itemsToAdd[i] = random.Next();
            itemsToRemove[i] = itemsToAdd[i];
        }
        // Fisher-Yates shuffle for random removal order
        for (int i = itemsToRemove.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = itemsToRemove[i];
            itemsToRemove[i] = itemsToRemove[j];
            itemsToRemove[j] = temp;
        }
        for (int i = 0; i < NUM_OPERATIONS; i++)
        {
            // 50% chance to access an existing item, 50% non-existing
            if (random.Next(0, 2) == 0)
                indicesToAccess[i] = itemsToAdd[random.Next(0, NUM_ITEMS)];
            else
                indicesToAccess[i] = random.Next();
        }

        // --- 运行测试 ---
        RunAllTests(itemsToAdd, indicesToAccess, itemsToRemove);
    }

    private void RunAllTests(int[] itemsToAdd, int[] indicesToAccess, int[] itemsToRemove)
    {
        // --- OrderList<int> Tests ---
        UnityEngine.Debug.Log("--- Testing OrderList<int> ---");
        var orderList = new OrderList<int>(NUM_ITEMS);
        RunPerformanceTest("Bulk Add (one by one)", () =>
        {
            foreach (var item in itemsToAdd) orderList.Add(item);
        });
        RunPerformanceTest("Random Access (Contains)", () =>
        {
            foreach (var index in indicesToAccess) orderList.Contains(index);
        });
        RunPerformanceTest("Iteration (foreach)", () =>
        {
            int count = 0;
            foreach (var item in orderList) count++;
        });
        RunPerformanceTest("Random Remove", () =>
        {
            foreach (var item in itemsToRemove) orderList.Remove(item);
        });

        // --- List<int> + Sort() Tests (Comparison 1) ---
        UnityEngine.Debug.Log("--- Testing List<int> + Sort() (Comparison) ---");
        var list = new List<int>(NUM_ITEMS);
        RunPerformanceTest("Bulk Add + Sort", () =>
        {
            foreach (var item in itemsToAdd) list.Add(item);

            list.Sort(); // Sorting is part of the "add" process for this test
        });
        RunPerformanceTest("Random Access (BinarySearch)", () =>
        {
            foreach (var index in indicesToAccess) list.BinarySearch(index);
        });
        RunPerformanceTest("Iteration (foreach)", () =>
        {
            int count = 0;
            foreach (var item in list) count++;
        });
        RunPerformanceTest("Random Remove (BinarySearch & Remove)", () =>
        {
            // Note: List.Remove is O(n), so this will be slow
            foreach (var item in itemsToRemove)
            {
                var index = list.BinarySearch(item);
                if (index >= 0)
                    list.Remove(item);
            }
        });

        // --- SortedSet<int> Tests (Comparison 2) ---
        UnityEngine.Debug.Log("--- Testing SortedSet<int> (Comparison) ---");
        var sortedSet = new SortedSet<int>();
        RunPerformanceTest("Bulk Add (one by one)", () =>
        {
            foreach (var item in itemsToAdd) sortedSet.Add(item);
        });
        RunPerformanceTest("Random Access (Contains)", () =>
        {
            foreach (var index in indicesToAccess) sortedSet.Contains(index);
        });
        RunPerformanceTest("Iteration (foreach)", () =>
        {
            int count = 0;
            foreach (var item in sortedSet) count++;
        });
        RunPerformanceTest("Random Remove", () =>
        {
            foreach (var item in itemsToRemove) sortedSet.Remove(item);
        });
    }
}