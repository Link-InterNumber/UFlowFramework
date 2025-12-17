using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 用于测试的 IBVHItem 实现
/// </summary>
public class TestBVHObject : IBVHItem
{
    public BoundingBox Bounds { get; set; }
    public Vector3 Position { get; set; }
    public string Name { get; set; } // 用于调试时识别对象

    public TestBVHObject(string name, Vector3 position, Vector3 size)
    {
        Name = name;
        Position = position;
        Bounds = new BoundingBox
        {
            Min = position - size / 2,
            Max = position + size / 2
        };
    }
}

public class BVHTest : RunTestMono
{
    void Start()
    {
        Debug.Log("========== BVH Test Suite Started ==========");

        // --- BoundingBox Tests ---
        TestBoundingBox_Intersects();
        TestBoundingBox_Expand();
        TestBoundingBox_Expand_NegativeCoords(); // 暴露 CalculateBoundingBox 的 bug

        // --- BVHTree Build Tests ---
        TestBuild_HandlesEmptyList();
        TestBuild_StackOverflow(); // 暴露 BuildRecursive 中的无限递归 bug
        TestBuild_CorrectLeafNodeObjects(); // 暴露叶子节点物体分配的 bug

        // --- BVHTree Query Tests ---
        TestQuery_FindsIntersectingObject();
        TestQuery_DoesNotFindNonIntersectingObject();
        TestQuery_FindsMultipleObjects();
        TestQuery_DeepHierarchy(); // 暴露查询递归逻辑的 bug

        Debug.Log("========== BVH Test Suite Finished ==========");
    }

    #region Test Cases

    // --- BoundingBox Tests ---
    void TestBoundingBox_Intersects()
    {
        RunTest("BoundingBox - Intersects", () =>
        {
            var boxA = new BoundingBox { Min = Vector3.zero, Max = Vector3.one };
            var boxB = new BoundingBox { Min = new Vector3(0.5f, 0.5f, 0.5f), Max = new Vector3(1.5f, 1.5f, 1.5f) };
            var boxC = new BoundingBox { Min = new Vector3(2, 2, 2), Max = new Vector3(3, 3, 3) };
            Assert(boxA.Intersects(boxB), "boxA and boxB should intersect.");
            Assert(!boxA.Intersects(boxC), "boxA and boxC should not intersect.");
        });
    }

    void TestBoundingBox_Expand()
    {
        RunTest("BoundingBox - Expand", () =>
        {
            var boxA = new BoundingBox { Min = Vector3.zero, Max = Vector3.one };
            var boxB = new BoundingBox { Min = new Vector3(-1, 2, -1), Max = new Vector3(2, 3, 2) };
            boxA.Expand(boxB);
            Assert(boxA.Min == new Vector3(-1, 0, -1), "Min should be (-1, 0, -1).");
            Assert(boxA.Max == new Vector3(2, 3, 2), "Max should be (2, 3, 2).");
        });
    }

    void TestBoundingBox_Expand_NegativeCoords()
    {
        RunTest("BoundingBox - Expand with Negative Coords (Exposes CalculateBoundingBox bug)", () =>
        {
            // 这个测试模拟了 CalculateBoundingBox 的行为
            var bounds = new BoundingBox { Min = Vector3.zero, Max = Vector3.zero }; // 初始包围盒
            var objectBox = new BoundingBox { Min = new Vector3(-10, -10, -10), Max = new Vector3(-5, -5, -5) };
            bounds.Expand(objectBox);
            Assert(bounds.Min == new Vector3(-10, -10, -10), "Min should be (-10, -10, -10).");
            // 你的 CalculateBoundingBox 会在这里失败
            Assert(bounds.Max == new Vector3(0, 0, 0), "Max should be (0, 0, 0).");
        });
    }

    // --- BVHTree Build Tests ---
    void TestBuild_HandlesEmptyList()
    {
        RunTest("Build - Handles Empty or Null List", () =>
        {
            var bvh = new BVHTree();
            bvh.Build(null); // Should not throw
            bvh.Build(new List<IBVHItem>()); // Should not throw
            Assert(true, "Build completed without errors.");
        });
    }

    void TestBuild_StackOverflow()
    {
        RunTest("Build - StackOverflow (Exposes BuildRecursive bug)", () =>
        {
            var bvh = new BVHTree();
            var objects = new List<IBVHItem>();
            // 使用超过5个物体来触发递归
            for (int i = 0; i < 10; i++)
            {
                objects.Add(new TestBVHObject($"Obj{i}", new Vector3(i, i, i), Vector3.one));
            }
            // 你的 BuildRecursive 会因为错误的终止条件 (objects.Count <= 5) 导致无限递归
            bvh.Build(objects);
            Assert(true, "If this test passes, the StackOverflowException was likely fixed.");
        });
    }

    void TestBuild_CorrectLeafNodeObjects()
    {
        RunTest("Build - Correct Leaf Node Objects (Exposes object assignment bug)", () =>
        {
            // 这个测试需要修复 StackOverflow bug 才能运行
            // 假设叶子节点阈值为2
            var bvh = new BVHTree();
            var objects = new List<IBVHItem>
            {
                new TestBVHObject("A", new Vector3(1,0,0), Vector3.one),
                new TestBVHObject("B", new Vector3(2,0,0), Vector3.one),
                new TestBVHObject("C", new Vector3(10,0,0), Vector3.one),
                new TestBVHObject("D", new Vector3(11,0,0), Vector3.one),
            };
            bvh.Build(objects);
            // 理想情况下，查询一个只包含 "A" 和 "B" 的区域，结果不应包含 "C" 和 "D"
            var results = bvh.QueryCollisions(new BoundingBox { Min = Vector3.zero, Max = new Vector3(3, 1, 1) });
            // 你的代码会失败，因为每个叶子节点都包含了所有4个对象
            Assert(results.Count == 2, $"Expected 2 results, but got {results.Count}. Leaf nodes may contain incorrect objects.");
        });
    }

    // --- BVHTree Query Tests ---
    void TestQuery_FindsIntersectingObject()
    {
        RunTest("Query - Finds Intersecting Object", () =>
        {
            var bvh = new BVHTree();
            var obj = new TestBVHObject("A", Vector3.zero, Vector3.one);
            bvh.Build(new List<IBVHItem> { obj });
            var queryBox = new BoundingBox { Min = new Vector3(-0.5f, -0.5f, -0.5f), Max = new Vector3(0.5f, 0.5f, 0.5f) };
            var results = bvh.QueryCollisions(queryBox);
            Assert(results.Count == 1 && results[0] == obj, "Should find the intersecting object.");
        });
    }

    void TestQuery_DoesNotFindNonIntersectingObject()
    {
        RunTest("Query - Does Not Find Non-Intersecting Object", () =>
        {
            var bvh = new BVHTree();
            var obj = new TestBVHObject("A", Vector3.zero, Vector3.one);
            bvh.Build(new List<IBVHItem> { obj });
            var queryBox = new BoundingBox { Min = new Vector3(5, 5, 5), Max = new Vector3(6, 6, 6) };
            var results = bvh.QueryCollisions(queryBox);
            Assert(results.Count == 0, "Should not find any objects.");
        });
    }

    void TestQuery_FindsMultipleObjects()
    {
        RunTest("Query - Finds Multiple Objects", () =>
        {
            var bvh = new BVHTree();
            var objects = new List<IBVHItem>
            {
                new TestBVHObject("A", new Vector3(1, 1, 1), Vector3.one),
                new TestBVHObject("B", new Vector3(10, 10, 10), Vector3.one), // 不相交
                new TestBVHObject("C", new Vector3(2, 2, 2), Vector3.one)
            };
            bvh.Build(objects);
            var queryBox = new BoundingBox { Min = Vector3.zero, Max = new Vector3(5, 5, 5) };
            var results = bvh.QueryCollisions(queryBox);
            Assert(results.Count == 2, $"Expected 2 results, but got {results.Count}.");
            Assert(results.Any(o => ((TestBVHObject)o).Name == "A") && results.Any(o => ((TestBVHObject)o).Name == "C"), "Should contain A and C.");
        });
    }

    void TestQuery_DeepHierarchy()
    {
        RunTest("Query - Deep Hierarchy (Exposes query recursion bug)", () =>
        {
            var bvh = new BVHTree();
            var objects = new List<IBVHItem>
            {
                new TestBVHObject("A", new Vector3(1, 1, 1), Vector3.one), // 在左子树
                new TestBVHObject("B", new Vector3(100, 100, 100), Vector3.one) // 在右子树
            };
            bvh.Build(objects);
            // 查询一个只与右子树相交的区域
            var queryBox = new BoundingBox { Min = new Vector3(99, 99, 99), Max = new Vector3(101, 101, 101) };
            var results = bvh.QueryCollisions(queryBox);
            // 你的 QueryRecursive 方法因为逻辑错误，永远无法递归到子节点，所以这里会失败
            Assert(results.Count == 1 && ((TestBVHObject)results[0]).Name == "B", "Should find object B in a deep part of the tree.");
        });
    }

    #endregion
}