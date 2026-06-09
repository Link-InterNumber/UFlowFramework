using UnityEngine;
using PowerCellStudio;
using System.Linq;

public class SparseSetComprehensiveTest : RunTestMono
{
    void Start()
    {
        Debug.Log("========== SparseSet Comprehensive Test Suite Started ==========");
        
        // --- Basic Functionality ---
        TestAddAndCount();
        TestContains();
        TestFindAndIndexer();
        TestUpdate();
        TestClear();

        // --- Remove Logic ---
        TestRemoveSingleItem();
        TestRemoveFromMiddle();
        TestRemoveLastItem();
        TestRemoveFirstItem();
        TestRemoveNonExistent();

        // --- Edge Cases & Error Handling ---
        TestAddWithIndexZero();
        TestAddWithNegativeIndex();
        TestAddNullItem();
        TestDenseArrayResize();
        TestSparseArrayResize();

        // --- ICollection & IEnumerable Interface ---
        TestEnumerator();
        TestCopyTo();

        Debug.Log("========== SparseSet Comprehensive Test Suite Finished ==========");
    }

    #region Test Cases

    void TestAddAndCount()
    {
        RunTest("Add and Count", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "A"));
            Assert(set.Count == 1, "Count should be 1 after adding one item.");
            set.Add(new TestItem(20, "B"));
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
            var indexedItem = set[0];
            Assert(indexedItem != null && indexedItem.data == "D", "Indexer should return the correct item.");
        });
    }
    
    void TestUpdate()
    {
        RunTest("Update Existing Item", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(15, "E_old"));
            set.Add(new TestItem(15, "E_new"));
            Assert(set.Count == 1, "Count should remain 1 after update.");
            Assert(set.FindOrDefault(15).data == "E_new", "Item data should be updated.");
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

    void TestRemoveSingleItem()
    {
        RunTest("Remove - Single Item", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(100, "Single"));
            set.Remove(100);
            Assert(set.Count == 0, "Count should be 0.");
            Assert(!set.Contains(100), "Set should be empty.");
        });
    }

    void TestRemoveFromMiddle()
    {
        RunTest("Remove - From Middle", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "A"));
            set.Add(new TestItem(20, "B")); // Item to remove
            set.Add(new TestItem(30, "C")); // This will be swapped
            set.Remove(20);
            Assert(set.Count == 2, "Count should be 2.");
            Assert(!set.Contains(20), "Should not contain removed item.");
            Assert(set.Contains(10), "Item A should still exist.");
            Assert(set.Contains(30), "Item C (swapped) should still exist.");
            Assert(set.FindOrDefault(30).data == "C", "Item C data should be correct after swap.");
        });
    }

    void TestRemoveLastItem()
    {
        RunTest("Remove - Last Item", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "A"));
            set.Add(new TestItem(20, "B"));
            set.Remove(20); // Remove the last added item
            Assert(set.Count == 1, "Count should be 1.");
            Assert(!set.Contains(20), "Should not contain removed item.");
            Assert(set.Contains(10), "Item A should still exist.");
        });
    }

    void TestRemoveFirstItem()
    {
        RunTest("Remove - First Item", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "A")); // Item to remove
            set.Add(new TestItem(20, "B"));
            set.Add(new TestItem(30, "C")); // This will be swapped
            set.Remove(10);
            Assert(set.Count == 2, "Count should be 2.");
            Assert(!set.Contains(10), "Should not contain removed item.");
            Assert(set.Contains(20), "Item B should still exist.");
            Assert(set.Contains(30), "Item C (swapped) should still exist.");
        });
    }

    void TestRemoveNonExistent()
    {
        RunTest("Remove - Non-Existent Item", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "A"));
            bool result = set.Remove(999);
            Assert(!result, "Remove should return false for non-existent item.");
            Assert(set.Count == 1, "Count should not change.");
        });
    }

    void TestAddWithIndexZero()
    {
        RunTest("Edge Case - Add with Index 0", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(0, "Zero"));
            Assert(set.Count == 1, "Count should be 1.");
            Assert(set.Contains(0), "Should contain index 0.");
            Assert(set.FindOrDefault(0).data == "Zero", "Item at index 0 should be correct.");
        });
    }

    void TestAddWithNegativeIndex()
    {
        RunTest("Edge Case - Add with Negative Index", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(-1, "Negative"));
            Assert(set.Count == 0, "Count should be 0, negative index should be ignored.");
        });
    }

    void TestAddNullItem()
    {
        RunTest("Edge Case - Add Null Item", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(null);
            Assert(set.Count == 0, "Count should be 0, null item should be ignored.");
        });
    }

    void TestDenseArrayResize()
    {
        RunTest("Edge Case - Dense Array Resize", () =>
        {
            var set = new SparseSet<TestItem>(5); // Small page size
            for (int i = 0; i < 10; i++)
            {
                set.Add(new TestItem(i, $"Item_{i}"));
            }
            Assert(set.Count == 10, "Count should be 10 after dense resize.");
            Assert(set.Contains(9), "Should contain last added item.");
            Assert(set.FindOrDefault(9).data == "Item_9", "Data of last item should be correct.");
        });
    }

    void TestSparseArrayResize()
    {
        RunTest("Edge Case - Sparse Array Resize", () =>
        {
            var set = new SparseSet<TestItem>(10); // Small page size
            set.Add(new TestItem(100, "BigIndex"));
            Assert(set.Count == 1, "Count should be 1.");
            Assert(set.Contains(100), "Should contain item with large index after sparse resize.");
        });
    }

    void TestEnumerator()
    {
        RunTest("IEnumerable - Enumerator", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(10, "K"));
            set.Add(new TestItem(0, "L"));
            set.Add(new TestItem(30, "M"));
            var items = set.ToList(); // Using Linq's ToList() calls the enumerator
            Assert(items.Count == 3, "Enumerator should yield 3 items.");
            Assert(items.Any(i => i.data == "K"), "Enumerator result should contain item K.");
            Assert(items.Any(i => i.data == "L"), "Enumerator result should contain item L.");
            Assert(items.Any(i => i.data == "M"), "Enumerator result should contain item M.");
        });
    }

    void TestCopyTo()
    {
        RunTest("ICollection - CopyTo", () =>
        {
            var set = new SparseSet<TestItem>();
            set.Add(new TestItem(5, "X"));
            set.Add(new TestItem(15, "Y"));
            var array = new TestItem[2];
            set.CopyTo(array, 0);
            Assert(array.Length == 2, "Array length should be 2.");
            Assert(array.Any(i => i != null && i.data == "X"), "Array should contain item X.");
            Assert(array.Any(i => i != null && i.data == "Y"), "Array should contain item Y.");
            Assert(array[0] != null, "First element of copied array should not be null.");
        });
    }

    #endregion
}