using UnityEngine;
using PowerCellStudio;
using System.Linq;
using System;
using System.Reflection;

public class BinaryTreeTest : RunTestMono
{
    public class BinaryTreeTestItem : IComparable<BinaryTreeTestItem>, IDelta<BinaryTreeTestItem>
    {

        public BinaryTreeTestItem(int initV)
        {
            v = initV;
        }
        public int v;

        public int CompareTo(BinaryTreeTestItem other)
        {
            return v.CompareTo(other.v);
        }

        public int DeltaTo(BinaryTreeTestItem other)
        {
            return v - other.v;
        }
    }

    void Start()
    {
        Debug.Log("========== BinaryTree<T> Test Suite Started ==========");

        // --- Basic Functionality ---
        TestInsertAndCount();
        TestRemove();
        TestClear();

        // --- Build Functionality (Critical Tests) ---
        TestBuild_WithOddNumberOfNodes();
        TestBuild_WithEvenNumberOfNodes();
        TestBuild_AfterInsertAndRemove();
        TestBuild_WithEmptyList();

        // --- IEnumerable Interface ---
        TestEnumerator();
        TestFind();

        Debug.Log("========== BinaryTree<T> Test Suite Finished ==========");
    }

    #region Test Cases

    void TestInsertAndCount()
    {
        RunTest("Insert and Count", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            tree.Insert(new BinaryTreeTestItem(10));
            Assert(tree.Count == 1, "Count should be 1 after one insert.");
            tree.Insert(new BinaryTreeTestItem(20));
            Assert(tree.Count == 2, "Count should be 2 after two inserts.");
        });
    }

    void TestRemove()
    {
        RunTest("Remove", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            var itemsToRemove = new BinaryTreeTestItem(10);
            tree.Insert(itemsToRemove);
            tree.Insert(new BinaryTreeTestItem(20));
            bool result = tree.Remove(itemsToRemove);
            Assert(result, "Remove should return true for an existing item.");
            Assert(tree.Count == 1, "Count should be 1 after removing an item.");
            Assert(!tree.AsEnumerable().Contains(itemsToRemove), "Tree should not contain the removed item.");
        });
    }

    void TestClear()
    {
        RunTest("Clear", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            tree.Insert(new BinaryTreeTestItem(10));
            tree.Insert(new BinaryTreeTestItem(20));
            tree.Clear();
            Assert(tree.Count == 0, "Count should be 0 after Clear.");
            Assert(tree.AsEnumerable().Count() == 0, "Enumerator should yield no items after Clear.");
        });
    }

    // --- Build Tests ---

    void TestBuild_WithOddNumberOfNodes()
    {
        RunTest("Build - With Odd Number of Nodes (e.g., 7)", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            var data = new[] { 10, 20, 30, 40, 50, 60, 70 };
            foreach (var i in data) tree.Insert(new BinaryTreeTestItem(i));

            tree.Build();

            // Use reflection to inspect the private _root field
            var root = GetRootNode(tree);
            Assert(root != null, "Root should not be null after build.");

            // Expected structure for a balanced tree from a sorted list {10,20,30,40,50,60,70}
            // Root should be 40
            // Left subtree root should be 20
            // Right subtree root should be 60
            Assert(GetValue(root) == 40, "Root value should be the median (40).");
            Assert(GetValue(GetLeft(root)) == 20, "Left child of root should be 20.");
            Assert(GetValue(GetRight(root)) == 60, "Right child of root should be 60.");
            Assert(GetValue(GetLeft(GetLeft(root))) == 10, "Leaf node should be 10.");
            Assert(GetValue(GetRight(GetLeft(root))) == 30, "Leaf node should be 30.");
            Assert(GetValue(GetLeft(GetRight(root))) == 50, "Leaf node should be 50.");
            Assert(GetValue(GetRight(GetRight(root))) == 70, "Leaf node should be 70.");
        });
    }

    void TestBuild_WithEvenNumberOfNodes()
    {
        RunTest("Build - With Even Number of Nodes (e.g., 6)", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            var data = new[] { 10, 20, 30, 40, 50, 60 };
            foreach (var i in data) tree.Insert(new BinaryTreeTestItem(i));

            tree.Build();
            var root = GetRootNode(tree);

            // Expected structure for {10,20,30,40,50,60}
            // Your implementation chooses floor, so root is 30
            Assert(GetValue(root) == 30, "Root value should be the lower median (30).");
            Assert(GetValue(GetLeft(root)) == 10, "Left child of root should be 10.");
            Assert(GetValue(GetRight(GetLeft(root))) == 20, "Right child of 10 should be 20.");
            Assert(GetValue(GetRight(root)) == 50, "Right child of root should be 50.");
            Assert(GetValue(GetLeft(GetRight(root))) == 40, "Left child of 50 should be 40.");
            Assert(GetValue(GetRight(GetRight(root))) == 60, "Right child of 50 should be 60.");
        });
    }

    void TestBuild_AfterInsertAndRemove()
    {
        RunTest("Build - After Insert and Remove operations", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            tree.Insert(new BinaryTreeTestItem(10));
            var toRemove = new BinaryTreeTestItem(50);
            tree.Insert(toRemove);
            tree.Insert(new BinaryTreeTestItem(20));
            tree.Insert(new BinaryTreeTestItem(40));
            tree.Insert(new BinaryTreeTestItem(30));
            tree.Remove(toRemove); // Data is now {10, 20, 30, 40}

            tree.Build();
            var root = GetRootNode(tree);

            // Expected structure for {10, 20, 30, 40}
            // Your implementation chooses floor, so root is 20
            Assert(GetValue(root) == 20, "Root should be 20.");
            Assert(GetValue(GetLeft(root)) == 10, "Left child should be 10.");
            Assert(GetValue(GetRight(root)) == 30, "Right child should be 30.");
            Assert(GetValue(GetRight(GetRight(root))) == 40, "Grandchild should be 40.");
        });
    }

    void TestBuild_WithEmptyList()
    {
        RunTest("Build - With Empty List", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            tree.Build(); // Should not throw an exception
            var root = GetRootNode(tree);
            Assert(root == null, "Root should be null for an empty tree.");
        });
    }

    void TestEnumerator()
    {
        RunTest("Enumerator", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            tree.Insert(new BinaryTreeTestItem(30));
            tree.Insert(new BinaryTreeTestItem(10));
            tree.Insert(new BinaryTreeTestItem(20));

            var list = tree.AsEnumerable().ToList();
            // Note: The enumerator iterates over _rawData, which is not guaranteed to be sorted until Build() is called.
            Assert(list.Count == 3, "Enumerator should yield 3 items.");
            Assert(list.Any(o => o.v == 10), "Enumerator should contain item 10.");
            Assert(list.Any(o => o.v == 20), "Enumerator should contain item 20.");
            Assert(list.Any(o => o.v == 30), "Enumerator should contain item 30.");

            tree.Build();
            list = tree.AsEnumerable().ToList();
            Assert(list[0].v == 10 && list[1].v == 20 && list[2].v == 30, "Enumerator should yield items in sorted order after Build().");
        });
    }

    void TestFind()
    {
        RunTest("Find", () =>
        {
            var tree = new BinaryTree<BinaryTreeTestItem>();
            tree.Insert(new BinaryTreeTestItem(10));
            tree.Insert(new BinaryTreeTestItem(20));
            tree.Insert(new BinaryTreeTestItem(30));
            tree.Insert(new BinaryTreeTestItem(50));
            tree.Insert(new BinaryTreeTestItem(70));
            tree.Build();

            var found = tree.Find(new BinaryTreeTestItem(20));
            Assert(found.v == 20, "Find should return the correct item.");

            var nearest = tree.Find(new BinaryTreeTestItem(60));
            Assert(nearest.v == 50, $"Find should return the nearest item (50), got {nearest} instead.");
        });
    }

    #endregion

    #region Reflection Helpers
    // These helpers use reflection to access private members for testing purposes.

    private object GetRootNode(BinaryTree<BinaryTreeTestItem> tree)
    {
        FieldInfo field = typeof(BinaryTree<BinaryTreeTestItem>).GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance);
        return field.GetValue(tree);
    }

    private int GetValue(object node)
    {
        if (node == null) throw new NullReferenceException("Node is null.");
        FieldInfo field = node.GetType().GetField("valueT", BindingFlags.Public | BindingFlags.Instance);
        return (field.GetValue(node) as BinaryTreeTestItem).v;
    }

    private object GetLeft(object node)
    {
        if (node == null) return null;
        FieldInfo field = node.GetType().GetField("left", BindingFlags.Public | BindingFlags.Instance);
        return field.GetValue(node);
    }

    private object GetRight(object node)
    {
        if (node == null) return null;
        FieldInfo field = node.GetType().GetField("right", BindingFlags.Public | BindingFlags.Instance);
        return field.GetValue(node);
    }
    #endregion
}