using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Diagnostics;

public class BVHPerformanceTest : RunTestMono
{
    // --- 测试参数 ---
    private const int NUM_OBJECTS = 20000; // 场景中的物体数量
    private const int NUM_QUERIES = 5000;  // 执行的查询次数
    private const float SCENE_SIZE = 500f; // 场景范围大小
    private const float OBJECT_MAX_SIZE = 5f;  // 物体最大尺寸
    private const float QUERY_BOX_SIZE = 10f; // 查询盒大小

    private List<IBVHItem> _testObjects;
    private List<BoundingBox> _queryBoxes;
    private BVHTree _bvhTree;

    void Start()
    {
        UnityEngine.Debug.Log($"========== BVH Performance Test Started (Objects: {NUM_OBJECTS}, Queries: {NUM_QUERIES}) ==========");

        // 1. 准备测试数据
        PrepareTestData();

        // 2. 运行所有测试
        RunAllTests();
    }

    /// <summary>
    /// 生成用于测试的随机物体和查询盒
    /// </summary>
    void PrepareTestData()
    {
        var stopwatch = Stopwatch.StartNew();
        _testObjects = new List<IBVHItem>(NUM_OBJECTS);
        _queryBoxes = new List<BoundingBox>(NUM_QUERIES);
        _bvhTree = new BVHTree();

        var random = new System.Random();

        // 生成物体
        for (int i = 0; i < NUM_OBJECTS; i++)
        {
            var pos = new Vector3(
                (float)(random.NextDouble() * SCENE_SIZE) - SCENE_SIZE / 2,
                (float)(random.NextDouble() * SCENE_SIZE) - SCENE_SIZE / 2,
                (float)(random.NextDouble() * SCENE_SIZE) - SCENE_SIZE / 2
            );
            var size = new Vector3(
                (float)(random.NextDouble() * OBJECT_MAX_SIZE) + 1f,
                (float)(random.NextDouble() * OBJECT_MAX_SIZE) + 1f,
                (float)(random.NextDouble() * OBJECT_MAX_SIZE) + 1f
            );
            _testObjects.Add(new TestBVHObject($"Obj_{i}", pos, size));
        }

        // 生成查询盒
        for (int i = 0; i < NUM_QUERIES; i++)
        {
            var pos = new Vector3(
                (float)(random.NextDouble() * SCENE_SIZE) - SCENE_SIZE / 2,
                (float)(random.NextDouble() * SCENE_SIZE) - SCENE_SIZE / 2,
                (float)(random.NextDouble() * SCENE_SIZE) - SCENE_SIZE / 2
            );
            var size = Vector3.one * QUERY_BOX_SIZE;
            _queryBoxes.Add(new BoundingBox { Min = pos - size / 2, Max = pos + size / 2 });
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Test data prepared in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
    }

    void RunAllTests()
    {
        // --- BVH 构建性能测试 ---
        RunPerformanceTest("BVH Build Performance", () =>
        {
            _bvhTree.Build(_testObjects);
        });

        // --- BVH 查询性能测试 ---
        int bvhResultCount = 0;
        RunPerformanceTest("BVH Query Performance", () =>
        {
            foreach (var queryBox in _queryBoxes)
            {
                var results = _bvhTree.QueryCollisions(queryBox);
                bvhResultCount += results.Count;
            }
        });
        UnityEngine.Debug.Log($"    -> BVH found {bvhResultCount} total collisions.");


        // --- 暴力破解查询性能测试 (用于对比) ---
        int bruteForceResultCount = 0;
        RunPerformanceTest("Brute-Force Query Performance (Comparison)", () =>
        {
            foreach (var queryBox in _queryBoxes)
            {
                var results = new List<IBVHItem>();
                foreach (var obj in _testObjects)
                {
                    if (obj.Bounds.Intersects(queryBox))
                    {
                        results.Add(obj);
                    }
                }
                bruteForceResultCount += results.Count;
            }
        });
        UnityEngine.Debug.Log($"    -> Brute-force found {bruteForceResultCount} total collisions.");
    }
}