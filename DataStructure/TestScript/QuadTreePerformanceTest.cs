using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class QuadTreePerformanceTest : RunTestMono
{
    // --- 测试参数 ---
    private const int NUM_OBJECTS = 50000; // 场景中的物体数量
    private const int NUM_OPERATIONS = 10000; // 执行查找/移除/获取操作的次数
    private const float SCENE_EXTENTS = 500f; // 场景范围 (中心点到边缘的距离)

    private List<TestQuadItem> _testObjects;
    private List<Vector2> _queryPositions;
    private List<TestQuadItem> _itemsToRemove;

    void Start()
    {
        UnityEngine.Debug.Log($"========== QuadTree Performance Test Started (Objects: {NUM_OBJECTS}, Operations: {NUM_OPERATIONS}) ==========");
        UnityEngine.Debug.Log("Preparing test data...");

        // 1. 准备测试数据
        PrepareTestData();
        UnityEngine.Debug.Log("Test data prepared. Running tests...");

        // 2. 运行所有测试
        RunAllTests();
    }

    /// <summary>
    /// 生成用于测试的随机物体和查询位置
    /// </summary>
    void PrepareTestData()
    {
        _testObjects = new List<TestQuadItem>(NUM_OBJECTS);
        _queryPositions = new List<Vector2>(NUM_OPERATIONS);
        var random = new System.Random();

        // 生成物体
        for (int i = 0; i < NUM_OBJECTS; i++)
        {
            _testObjects.Add(new TestQuadItem(
                string.Empty,
                position: new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * SCENE_EXTENTS,
                    (float)(random.NextDouble() * 2 - 1) * SCENE_EXTENTS
                )
            ));
        }

        // 生成查询位置
        for (int i = 0; i < NUM_OPERATIONS; i++)
        {
            _queryPositions.Add(new Vector2(
                (float)(random.NextDouble() * 2 - 1) * SCENE_EXTENTS,
                (float)(random.NextDouble() * 2 - 1) * SCENE_EXTENTS
            ));
        }
        
        // 选取要移除的物体
        _itemsToRemove = _testObjects.OrderBy(x => random.Next()).Take(NUM_OPERATIONS).ToList();
    }

    void RunAllTests()
    {
        // --- QuadTree 测试 ---
        UnityEngine.Debug.Log("--- Testing QuadTree<TestQuadItem> ---");
        var quadTree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * SCENE_EXTENTS, maxCount: 16, maxLv: 8);

        RunPerformanceTest("1. QuadTree Insert", () =>
        {
            foreach (var obj in _testObjects)
            {
                quadTree.Insert(obj);
            }
        });

        RunPerformanceTest("2. QuadTree Find Nearest", () =>
        {
            int matched = 0;
            foreach (var pos in _queryPositions)
            {
                var found = quadTree.Find(pos, true);
                if (found != null) matched++;
            }
            UnityEngine.Debug.Log($"    Found {matched} / {NUM_OPERATIONS} items.");
        });

        RunPerformanceTest("2. QuadTree Find Exact", () =>
        {
            int matched = 0;
            foreach (var pos in _queryPositions)
            {
                var found = quadTree.Find(pos, false);
                if (found != null) matched++;
            }
            UnityEngine.Debug.Log($"    Found {matched} / {NUM_OPERATIONS} items.");
        });

        // --- 新增的 GetLeaf 和 GetBlock 性能测试 ---
        RunPerformanceTest("3. QuadTree GetLeaf", () =>
        {
            int totalCount = 0;
            foreach (var pos in _queryPositions)
            {
                quadTree.GetLeaf(pos, out var count);
                totalCount += count;
            }
            UnityEngine.Debug.Log($"    Total items in leaves accessed: {totalCount}");
        });

        RunPerformanceTest("4. QuadTree GetBlock", () =>
        {
            int totalCount = 0;
            foreach (var pos in _queryPositions)
            {
                var block = quadTree.GetBlock(pos);
                totalCount += block.Count;
            }
            UnityEngine.Debug.Log($"    Total items in blocks accessed: {totalCount}");
        });
        // --- 结束新增部分 ---
        
        RunPerformanceTest("5. QuadTree Remove", () =>
        {
            foreach (var item in _itemsToRemove)
            {
                quadTree.Remove(item);
            }
        });

        // --- 暴力破解 (List<T>) 对比测试 ---
        UnityEngine.Debug.Log("--- Testing Brute-Force List<TestQuadItem> (Comparison) ---");
        var list = new List<TestQuadItem>(_testObjects);

        RunPerformanceTest("1. List Add (N/A, data already prepared)", () => { /* No-op */ });

        RunPerformanceTest("2. Brute-Force Find Nearest", () =>
        {
            int matched = 0;
            foreach (var pos in _queryPositions)
            {
                var found = FindNearestBruteForce(list, pos);
                if (found != null) matched++;
            }
            UnityEngine.Debug.Log($"    Found {matched} / {NUM_OPERATIONS} items.");
        });
        
        // GetLeaf 和 GetBlock 没有直接的暴力破解对等操作，它们是数据结构特有的功能。
        // 它们本身的性能就是衡量标准。
        UnityEngine.Debug.Log("[N/A] Brute-Force GetLeaf");
        UnityEngine.Debug.Log("[N/A] Brute-Force GetBlock");

        RunPerformanceTest("5. List Remove", () =>
        {
            foreach (var item in _itemsToRemove)
            {
                list.Remove(item);
            }
        });
    }

    /// <summary>
    /// 暴力破解方式查找最近的物体
    /// </summary>
    private TestQuadItem FindNearestBruteForce(List<TestQuadItem> objects, Vector2 pos)
    {
        if (objects.Count == 0) return null;
        
        TestQuadItem nearest = null;
        float minDistanceSq = float.MaxValue;

        foreach (var obj in objects)
        {
            float distSq = Vector2.SqrMagnitude(obj.Position - pos);
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                nearest = obj;
            }
        }
        return nearest;
    }
}