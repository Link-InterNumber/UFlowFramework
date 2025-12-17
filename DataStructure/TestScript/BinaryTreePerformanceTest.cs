using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

/// <summary>
/// 用于性能测试的值类型，实现了 BinaryTree 所需的接口。
/// </summary>
public struct BinaryTreePerformanceTestItem : IComparable<BinaryTreePerformanceTestItem>, IDelta<BinaryTreePerformanceTestItem>
{
    public int Value;

    public BinaryTreePerformanceTestItem(int value)
    {
        Value = value;
    }

    public int CompareTo(BinaryTreePerformanceTestItem other)
    {
        return Value.CompareTo(other.Value);
    }

    public int DeltaTo(BinaryTreePerformanceTestItem other)
    {
        // 返回差值的绝对值
        return Math.Abs(Value - other.Value);
    }

    public override string ToString() => Value.ToString();
}

public class BinaryTreePerformanceTest : RunTestMono
{
    // --- 测试参数 ---
    private const int INITIAL_BUILD_ITEMS = 20000;
    // 动态添加/移除的元素数量。注意：此值不宜过大，因为每次操作都会触发O(n log n)的重建。
    private const int DYNAMIC_OPERATIONS = 100;
    // 查找操作的次数
    private const int FIND_OPERATIONS = 10000;

    void Start()
    {
        UnityEngine.Debug.Log($"========== BinaryTree Performance Test Started (Initial: {INITIAL_BUILD_ITEMS}, Dynamic: {DYNAMIC_OPERATIONS}) ==========");
        UnityEngine.Debug.LogWarning("NOTE: Dynamic Add/Remove tests will be very slow due to the auto-rebuild design.");

        // --- 准备测试数据 ---
        var random = new System.Random();
        var initialItems = Enumerable.Range(0, INITIAL_BUILD_ITEMS).Select(i => new BinaryTreePerformanceTestItem(random.Next(0, INITIAL_BUILD_ITEMS * 2))).Distinct().ToArray();
        var dynamicAddItems = Enumerable.Range(0, DYNAMIC_OPERATIONS).Select(i => new BinaryTreePerformanceTestItem(random.Next(INITIAL_BUILD_ITEMS * 2, INITIAL_BUILD_ITEMS * 3))).Distinct().ToArray();
        var itemsToRemove = initialItems.OrderBy(x => random.Next()).Take(DYNAMIC_OPERATIONS).ToArray();
        var itemsToFind = Enumerable.Range(0, FIND_OPERATIONS).Select(i => new BinaryTreePerformanceTestItem(random.Next(0, INITIAL_BUILD_ITEMS * 2))).ToArray();

        // --- 运行测试 ---
        RunAllTests(initialItems, dynamicAddItems, itemsToRemove, itemsToFind);
    }

    private void RunAllTests(BinaryTreePerformanceTestItem[] initialItems, BinaryTreePerformanceTestItem[] dynamicAddItems, BinaryTreePerformanceTestItem[] itemsToRemove, BinaryTreePerformanceTestItem[] itemsToFind)
    {
        // --- BinaryTree<TestValue> Tests ---
        UnityEngine.Debug.Log("--- Testing BinaryTree<TestValue> ---");
        var binaryTree = new BinaryTree<BinaryTreePerformanceTestItem>();
        
        RunPerformanceTest("1. Initial Insert (Pre-Build)", () => {
            foreach (var item in initialItems) binaryTree.Insert(item);
        });
        RunPerformanceTest("2. First Build()", () => {
            binaryTree.Build();
        });
        RunPerformanceTest("3. Find Closest", () => {
            foreach (var item in itemsToFind) binaryTree.Find(item);
        });
        RunPerformanceTest("4. Iteration (foreach)", () => {
            int count = 0;
            foreach (var item in binaryTree) count++;
        });
        RunPerformanceTest("5. Dynamic Add (Auto-Rebuild)", () => {
            foreach (var item in dynamicAddItems) binaryTree.Insert(item);
        });
        RunPerformanceTest("6. Dynamic Remove (Auto-Rebuild)", () => {
            foreach (var item in itemsToRemove) binaryTree.Remove(item);
        });

        // --- SortedSet<TestValue> Tests (For Comparison) ---
        UnityEngine.Debug.Log("--- Testing SortedSet<TestValue> (Comparison) ---");
        var sortedSet = new SortedSet<BinaryTreePerformanceTestItem>();
        
        RunPerformanceTest("1+2. Bulk Add (Equivalent to Insert + Build)", () => {
            foreach (var item in initialItems) sortedSet.Add(item);
        });
        RunPerformanceTest("3. Find Closest", () => {
            foreach (var item in itemsToFind) FindClosestInSortedSet(sortedSet, item);
        });
        RunPerformanceTest("4. Iteration (foreach)", () => {
            int count = 0;
            foreach (var item in sortedSet) count++;
        });
        RunPerformanceTest("5. Dynamic Add", () => {
            foreach (var item in dynamicAddItems) sortedSet.Add(item);
        });
        RunPerformanceTest("6. Dynamic Remove", () => {
            foreach (var item in itemsToRemove) sortedSet.Remove(item);
        });
    }

    /// <summary>
    /// 在 SortedSet 中模拟寻找最近似值。
    /// </summary>
    private BinaryTreePerformanceTestItem FindClosestInSortedSet(SortedSet<BinaryTreePerformanceTestItem> set, BinaryTreePerformanceTestItem target)
    {
        if (set.Count == 0) return default;

        // 获取大于等于 target 的第一个元素
        var greaterOrEqual = set.GetViewBetween(target, set.Max).FirstOrDefault();
        // 获取小于 target 的第一个元素
        var lessThan = set.GetViewBetween(set.Min, target).LastOrDefault();

        if (greaterOrEqual.Equals(default)) return lessThan;
        if (lessThan.Equals(default)) return greaterOrEqual;

        int delta1 = target.DeltaTo(greaterOrEqual);
        int delta2 = target.DeltaTo(lessThan);

        return delta1 < delta2 ? greaterOrEqual : lessThan;
    }
}