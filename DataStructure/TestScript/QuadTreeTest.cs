using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 用于测试的 IQuadTreeItem 实现
/// </summary>
public class TestQuadItem : IQuadTreeItem
{
    public string Name { get; set; }
    public Vector2 Position { get; set; }

    public TestQuadItem(string name, Vector2 position)
    {
        Name = name;
        Position = position;
    }

    public Vector2 ToVector() => Position;

    public override string ToString() => $"{Name} @ {Position}";
}

public class QuadTreeTest : RunTestMono
{
    void Start()
    {
        Debug.Log("========== QuadTree Test Suite Started ==========");

        // --- Basic Functionality ---
        TestInsertAndCount();
        TestRemove();
        TestFind_ExactAndNearest();
        TestClear();
        TestDuplicateInsert();

        // --- Core Logic & Bug Exposure ---
        TestSplitMechanism();
        TestRemove_TriggersMerge();
        TestMerge_LogicError();
        TestGetIndex_QuadrantLogic(); // 验证象限索引逻辑

        // --- Enumerator/Getter Tests ---
        TestGetLeafAndBlock_WhenNotSplit(); // 新增：测试未分裂时的行为
        TestGetLeafAndBlock_WhenSplit();    // 新增：测试分裂后的行为
        TestEnumerators();

        Debug.Log("========== QuadTree Test Suite Finished ==========");
    }

    #region Test Cases

    void TestInsertAndCount()
    {
        RunTest("Insert and Count", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 2);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            var itemB = new TestQuadItem("B", new Vector2(-10, -10));
            tree.Insert(itemA);
            tree.Insert(itemB);
            Assert(tree.Count == 2, "Count should be 2 after two inserts.");
        });
    }

    void TestRemove()
    {
        RunTest("Remove", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 2);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            tree.Insert(itemA);
            bool result = tree.Remove(itemA);
            Assert(result, "Remove should return true for an existing item.");
            Assert(tree.Count == 0, "Count should be 0 after removing the item.");
            Assert(tree.Find(new Vector2(10, 10), false) == null, "Tree should not find the removed item.");
        });
    }

    void TestFind_ExactAndNearest()
    {
        RunTest("Find - Exact and Nearest", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            var itemB = new TestQuadItem("B", new Vector2(50, 50));
            tree.Insert(itemA);
            tree.Insert(itemB);

            var foundExact = tree.Find(new Vector2(10, 10), false);
            Assert(foundExact == itemA, "Should find the exact item at (10, 10).");

            var foundNearest = tree.Find(new Vector2(12, 12), true);
            Assert(foundNearest == itemA, "Should find item A as the nearest to (12, 12).");
        });
    }

    void TestClear()
    {
        RunTest("Clear", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100);
            tree.Insert(new TestQuadItem("A", Vector2.one));
            tree.Clear();
            Assert(tree.Count == 0, "Count should be 0 after Clear.");
        });
    }

    void TestDuplicateInsert()
    {
        RunTest("Duplicate Insert", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100);
            var itemA = new TestQuadItem("A", Vector2.one);
            tree.Insert(itemA);
            tree.Insert(itemA); // Insert again
            Assert(tree.Count == 1, "Count should remain 1 after inserting a duplicate.");
        });
    }

    void TestSplitMechanism()
    {
        RunTest("Split Mechanism", () =>
        {
            // maxCount=2 means the 3rd item should trigger a split
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 2);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));  // Top-right
            var itemB = new TestQuadItem("B", new Vector2(20, 20));  // Top-right
            var itemC = new TestQuadItem("C", new Vector2(-10, -10)); // Bottom-left
            tree.Insert(itemA);
            tree.Insert(itemB);
            tree.Insert(itemC); // This should trigger a split

            // After split, A and B should be in one child node, C in another
            int countA, countB, countC;
            var leafA = tree.GetLeaf(itemA.Position, out countA);
            var leafB = tree.GetLeaf(itemB.Position, out countB);
            var leafC = tree.GetLeaf(itemC.Position, out countC);

            Assert(leafA == leafB, "Items A and B should be in the same leaf node after split.");
            Assert(leafA != leafC, "Items A and C should be in different leaf nodes.");
            Assert(countA == 2, "The leaf for A and B should contain 2 items.");
            Assert(countC == 1, "The leaf for C should contain 1 item.");
        });
    }

    void TestRemove_TriggersMerge()
    {
        RunTest("Remove - Triggers Merge", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 1);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            var itemB = new TestQuadItem("B", new Vector2(-10, -10));
            tree.Insert(itemA);
            tree.Insert(itemB); // Tree is now split

            // Verify it's split by checking leaf nodes
            int countA_before, countB_before;
            var leafA_before = tree.GetLeaf(itemA.Position, out countA_before);
            var leafB_before = tree.GetLeaf(itemB.Position, out countB_before);
            Assert(leafA_before != leafB_before, "Tree should be split before removal.");

            tree.Remove(itemB); // This should trigger a merge

            int countA_after, countB_after;
            var leafA_after = tree.GetLeaf(itemA.Position, out countA_after);
            var leafB_after = tree.GetLeaf(itemB.Position, out countB_after);
            // After merge, A and B's positions should point to the same leaf (the parent)
            Assert(leafA_after == leafB_after, "Tree should merge back into a single leaf node.");
            Assert(countA_after == 1, "The merged leaf should contain 1 item.");
        });
    }

    void TestMerge_LogicError()
    {
        RunTest("Merge", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 1);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            var itemB = new TestQuadItem("B", new Vector2(-10, -10));
            tree.Insert(itemA);
            tree.Insert(itemB); // Split
            tree.Remove(itemB); // Merge

            var found = tree.Find(itemA.Position, false);
            Assert(found != null, "Item A should still exist after merge.");
        });
    }

    void TestGetIndex_QuadrantLogic()
    {
        RunTest("GetIndex - Quadrant Logic", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 1);
            tree.Insert(new TestQuadItem("trigger split", Vector2.zero));
            tree.Insert(new TestQuadItem("trigger split 2", Vector2.one));

            // We need to access the private _root node to call GetIndex
            // This is a common practice in unit testing private logic
            var root = (typeof(QuadTree<TestQuadItem>).GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(tree));
            var getIndexMethod = root.GetType().GetMethod("GetIndex");

            // Your GetIndex logic: 0:BL, 1:UL, 2:DR, 3:UR
            Assert((int)getIndexMethod.Invoke(root, new object[] { new Vector2(-10, -10) }) == 0, "Bottom-Left should be index 0.");
            Assert((int)getIndexMethod.Invoke(root, new object[] { new Vector2(-10, 10) }) == 1, "Top-Left should be index 1.");
            Assert((int)getIndexMethod.Invoke(root, new object[] { new Vector2(10, -10) }) == 2, "Bottom-Right should be index 2.");
            Assert((int)getIndexMethod.Invoke(root, new object[] { new Vector2(10, 10) }) == 3, "Top-Right should be index 3.");
        });
    }
    void TestGetLeafAndBlock_WhenNotSplit()
    {
        RunTest("GetLeaf/GetBlock - When Tree is Not Split", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 4);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            var itemB = new TestQuadItem("B", new Vector2(-10, 10));
            tree.Insert(itemA);
            tree.Insert(itemB);

            // Test GetLeaf
            int leafCount;
            var leaf = tree.GetLeaf(itemA.Position, out leafCount);
            Assert(leafCount == 2, "When not split, leaf count should be total count (2).");
            Assert(leaf.Contains(itemA) && leaf.Contains(itemB), "When not split, leaf should contain all items.");

            // Test GetBlock - This will expose a NullReferenceException in your current code
            // because it assumes root.nodes is not null.
            var block = tree.GetBlock(itemA.Position);
            Assert(block.Count == 2, "When not split, block should contain all items.");
            Assert(block.Contains(itemA) && block.Contains(itemB), "Block should contain A and B.");
        });
    }

    void TestGetLeafAndBlock_WhenSplit()
    {
        RunTest("GetLeaf/GetBlock - When Tree is Split", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 2);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));   // Top-Right
            var itemB = new TestQuadItem("B", new Vector2(-10, 10));  // Top-Left
            var itemC = new TestQuadItem("C", new Vector2(-10, -10)); // Bottom-Left
            tree.Insert(itemA);
            tree.Insert(itemB);
            tree.Insert(itemC); // Triggers a split

            // Test GetLeaf for item A
            int leafCountA;
            var leafA = tree.GetLeaf(itemA.Position, out leafCountA);
            Assert(leafCountA == 1, "Leaf for item A should have count 1.");
            Assert(leafA[0] == itemA, "Leaf for item A should contain item A.");

            // Test GetLeaf for item B
            int leafCountB;
            var leafB = tree.GetLeaf(itemB.Position, out leafCountB);
            Assert(leafCountB == 1, "Leaf for item B should have count 1.");
            Assert(leafB[0] == itemB, "Leaf for item B should contain item B.");

            // Test GetBlock for item A's position
            var block = tree.GetBlock(itemA.Position);
            Assert(block.Count == 3, "Block should contain all 3 items from the parent's children.");
            Assert(block.Contains(itemA) && block.Contains(itemB) && block.Contains(itemC), "Block should contain A, B, and C.");
        });
    }
    
    void TestEnumerators()
    {
        RunTest("Enumerators", () =>
        {
            var tree = new QuadTree<TestQuadItem>(Vector2.zero, Vector2.one * 100, maxCount: 1);
            var itemA = new TestQuadItem("A", new Vector2(10, 10));
            var itemB = new TestQuadItem("B", new Vector2(-10, -10));
            tree.Insert(itemA);
            tree.Insert(itemB); // Split

            var leafEnum = tree.GetLeafEnumerator(itemA.Position).ToList();
            Assert(leafEnum.Count == 1 && leafEnum[0] == itemA, "Leaf enumerator should yield item A.");

            var blockEnum = tree.GetBlockEnumerator(itemA.Position).ToList();
            Assert(blockEnum.Count == 2, "Block enumerator should yield 2 items.");
            Assert(blockEnum.Contains(itemA) && blockEnum.Contains(itemB), "Block enumerator should contain A and B.");
        });
    }

    #endregion
}