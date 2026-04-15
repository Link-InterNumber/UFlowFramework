using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 用于测试的 IIndex 实现
/// </summary>
public class TestItem : IIndex
{
    private int _index;
    public int index { get => _index; private set => _index = value; }
    public string data;

    public TestItem(int id, string payload)
    {
        _index = id;
        data = payload;
    }

    public override string ToString()
    {
        return $"[Item Index: {index}, Data: '{data}']";
    }
}

public class SparseSetTest : RunTestMono
{
    void Start()
    {
        Debug.Log("========== SparseSet Test Suite Started ==========");
        
        TestAddAndCount();
        TestContains();
        TestFindAndIndexer();
        TestUpdate();
        TestRemove();
        TestClear();
        TestEnumerator();
        TestResize();

        Debug.Log("========== SparseSet Test Suite Finished ==========");
    }

    void TestAddAndCount()
    {
        RunTest("Add and Count", () =>
        {
            var set = new SparseSet<TestItem>();
            var item1 = new TestItem(10, "A");
            var item2 = new TestItem(20, "B");

            set.Add(item1);
            Assert(set.Count == 1, "Count should be 1 after adding one item.");

            set.Add(item2);
            Assert(set.Count == 2, "Count should be 2 after adding two items.");
        });
    }

    void TestContains()
    {
        RunTest("Contains", () =>
        {
            var set = new SparseSet<TestItem>();
            var item1 = new TestItem(5, "C");
            set.Add(item1);

            Assert(set.Contains(5), "Should contain index 5.");
            Assert(set.Contains(item1), "Should contain item1.");
            Assert(!set.Contains(99), "Should not contain index 99.");
        });
    }

    void TestFindAndIndexer()
    {
        RunTest("FindOrDefault and Indexer", () =>
        {
            var set = new SparseSet<TestItem>();
            var item1 = new TestItem(8, "D");
            set.Add(item1);

            var foundItem = set.FindOrDefault(8);
            Assert(foundItem != null && foundItem.data == "D", "FindOrDefault should return the correct item.");
            Assert(set.FindOrDefault(100) == null, "FindOrDefault for non-existent index should return null.");

            var indexedItem = set[8];
            Assert(indexedItem != null && indexedItem.data == "D", "Indexer should return the correct item.");

            bool threwException = false;
            try { var _ = set[101]; }
            catch (KeyNotFoundException) { threwException = true; }
            Assert(threwException, "Indexer should throw KeyNotFoundException for non-existent index.");
        });
    }
    
    void TestUpdate()
    {
        RunTest("Update Existing Item", () =>
        {
            var set = new SparseSet<TestItem>();
            var item1 = new TestItem(15, "E_old");
            var item2 = new TestItem(15, "E_new");
            
            set.Add(item1);
            Assert(set.Count == 1, "Count should be 1.");
            
            set.Add(item2); // Add item with the same index
            Assert(set.Count == 1, "Count should remain 1 after update.");
            
            var updatedItem = set.FindOrDefault(15);
            Assert(updatedItem.data == "E_new", "Item data should be updated.");
        });
    }

    void TestRemove()
    {
        RunTest("Remove", () =>
        {
            var set = new SparseSet<TestItem>();
            var item1 = new TestItem(1, "F");
            var item2 = new TestItem(2, "G");
            var item3 = new TestItem(3, "H");
            set.Add(item1);
            set.Add(item2);
            set.Add(item3);

            // Remove from the middle
            bool removed = set.Remove(2);
            Assert(removed, "Remove should return true for an existing item.");
            Assert(set.Count == 2, "Count should be 2 after removing one item.");
            Assert(!set.Contains(2), "Set should not contain the removed index 2.");
            Assert(set.FindOrDefault(3).data == "H", "Item 3 should still exist.");
            
            // The last item (item3) should now be at the position of the removed item (item2)
            // This is a key test for the swap-and-pop logic.
            Assert(set.FindOrDefault(3) != null, "The swapped item should be findable by its original index.");

            // Remove non-existent
            removed = set.Remove(99);
            Assert(!removed, "Remove should return false for a non-existent item.");
        });
    }

    void TestClear()
    {
        RunTest("Clear", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(1, "I"));
            set.Add(new TestItem(2, "J"));
            
            set.Clear();
            Assert(set.Count == 0, "Count should be 0 after Clear.");
            Assert(!set.Contains(1), "Set should not contain any items after Clear.");
        });
    }

    void TestEnumerator()
    {
        RunTest("Enumerator", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "K"));
            set.Add(new TestItem(20, "L"));
            set.Add(new TestItem(30, "M"));

            var items = new List<TestItem>();
            foreach (var item in set)
            {
                items.Add(item);
            }
            Assert(items.Count == 3, "Enumerator should yield 3 items.");
            Assert(items.Any(i => i.data == "K"), "Enumerator result should contain item K.");
            Assert(items.Any(i => i.data == "L"), "Enumerator result should contain item L.");
            Assert(items.Any(i => i.data == "M"), "Enumerator result should contain item M.");
        });
    }

    void TestResize()
    {
        RunTest("Array Resizing", () =>
        {
            // Assuming default pageSize is 128
            var set = new SparseSet<TestItem>(10); 
            
            // Test sparse array resize
            set.Add(new TestItem(100, "BigIndex"));
            Assert(set.Contains(100), "Should contain item with large index after sparse resize.");
            
            // Test dense array resize
            for (int i = 0; i < 15; i++)
            {
                set.Add(new TestItem(i, $"Dense_{i}"));
            }
            Assert(set.Count == 16, "Count should be correct after dense resize (15 + BigIndex).");
            Assert(set.Contains(14), "Should contain last added item after dense resize.");
        });
    }
}