using UnityEngine;
using PowerCellStudio;
using System.Collections.Generic;
using System.Linq;
using System;

public class OrderListTest : RunTestMono
{
    void Start()
    {
        Debug.Log("========== OrderList<T> Test Suite Started ==========");

        // --- Constructor Tests ---
        TestDefaultConstructor();
        TestConstructorWithSourceList();
        TestConstructorWithEmptyOrNullSource();

        // --- Add & Count Tests ---
        TestAdd_ToEmptyList();
        TestAdd_AtBeginning();
        TestAdd_AtEnd();
        TestAdd_InMiddle();
        TestAdd_Duplicates();
        TestAdd_Resize();
        TestAddRange();

        // --- Remove Tests ---
        TestRemove_Item();
        TestRemove_NonExistentItem();
        TestRemoveAt_Index();
        TestRemoveAt_FirstAndLast();

        // --- Access & Search Tests ---
        TestIndexer_Get();
        TestIndexer_SetThrowsException();
        TestContains();
        TestIndexOf();
        TestAsSpan();

        // --- Other ICollection/IList Methods ---
        TestClear();
        TestCopyTo();
        TestEnumerator();
        TestInsert_ThrowsException();

        Debug.Log("========== OrderList<T> Test Suite Finished ==========");
    }

    #region Test Cases

    // --- Constructor Tests ---
    void TestDefaultConstructor()
    {
        RunTest("Constructor - Default", () =>
        {
            var list = new OrderList<int>();
            Assert(list.Count == 0, "Default constructor should create an empty list.");
        });
    }

    void TestConstructorWithSourceList()
    {
        RunTest("Constructor - With Unsorted Source List", () =>
        {
            var source = new List<int> { 5, 1, 9, 3 };
            var list = new OrderList<int>(source);
            Assert(list.Count == 4, "Count should match source list count.");
            Assert(list[0] == 1 && list[1] == 3 && list[2] == 5 && list[3] == 9, "List should be sorted after construction.");
        });
    }

    void TestConstructorWithEmptyOrNullSource()
    {
        RunTest("Constructor - With Empty or Null Source", () =>
        {
            var list1 = new OrderList<int>(new List<int>());
            Assert(list1.Count == 0, "Constructor with empty list should result in an empty list.");
            var list2 = new OrderList<int>(null);
            Assert(list2.Count == 0, "Constructor with null list should result in an empty list.");
        });
    }

    // --- Add & Count Tests ---
    void TestAdd_ToEmptyList()
    {
        RunTest("Add - To Empty List", () =>
        {
            var list = new OrderList<int>();
            list.Add(10);
            Assert(list.Count == 1, "Count should be 1.");
            Assert(list[0] == 10, "The single item should be at index 0.");
        });
    }

    void TestAdd_AtBeginning()
    {
        RunTest("Add - At Beginning", () =>
        {
            var list = new OrderList<int> { 10, 20 };
            list.Add(5);
            Assert(list.Count == 3, "Count should be 3.");
            Assert(list[0] == 5, "New item should be at the beginning.");
        });
    }

    void TestAdd_AtEnd()
    {
        RunTest("Add - At End", () =>
        {
            var list = new OrderList<int> { 10, 20 };
            list.Add(30);
            Assert(list.Count == 3, "Count should be 3.");
            Assert(list[2] == 30, "New item should be at the end.");
        });
    }

    void TestAdd_InMiddle()
    {
        RunTest("Add - In Middle", () =>
        {
            var list = new OrderList<int> { 10, 30 };
            list.Add(20);
            Assert(list.Count == 3, "Count should be 3.");
            Assert(list[1] == 20, "New item should be in the middle.");
        });
    }

    void TestAdd_Duplicates()
    {
        RunTest("Add - Duplicates", () =>
        {
            var list = new OrderList<int> { 10, 20, 30 };
            list.Add(20);
            Assert(list.Count == 4, "Count should be 4.");
            Assert(list.Count(x => x == 20) == 2, "There should be two '20's.");
            Assert(list[1] == 20 && list[2] == 20, "Duplicates should be adjacent.");
        });
    }

    void TestAdd_Resize()
    {
        RunTest("Add - Triggers Resize", () =>
        {
            var list = new OrderList<int>(2); // Small initial size
            list.Add(10);
            list.Add(20);
            list.Add(5); // This should trigger a resize
            Assert(list.Count == 3, "Count should be 3 after resize.");
            Assert(list[0] == 5 && list[1] == 10 && list[2] == 20, "List should remain sorted after resize.");
        });
    }

    void TestAddRange()
    {
        RunTest("AddRange", () =>
        {
            var list = new OrderList<int> { 10, 40 };
            list.AddRange(new[] { 5, 50, 25 });
            Assert(list.Count == 5, "Count should be 5.");
            Assert(list.SequenceEqual(new[] { 5, 10, 25, 40, 50 }), "List should be fully sorted after AddRange.");
        });
    }

    // --- Remove Tests ---
    void TestRemove_Item()
    {
        RunTest("Remove - Existing Item", () =>
        {
            var list = new OrderList<int> { 5, 10, 15, 20 };
            bool result = list.Remove(10);
            Assert(result, "Remove should return true for an existing item.");
            Assert(list.Count == 3, "Count should be 3.");
            Assert(!list.Contains(10), "List should not contain the removed item.");
            Assert(list.SequenceEqual(new[] { 5, 15, 20 }), "List should remain sorted.");
        });
    }

    void TestRemove_NonExistentItem()
    {
        RunTest("Remove - Non-Existent Item", () =>
        {
            var list = new OrderList<int> { 10, 20 };
            bool result = list.Remove(15);
            Assert(!result, "Remove should return false for a non-existent item.");
            Assert(list.Count == 2, "Count should not change.");
        });
    }

    void TestRemoveAt_Index()
    {
        RunTest("RemoveAt - Valid Index", () =>
        {
            var list = new OrderList<int> { 5, 10, 15 };
            list.RemoveAt(1);
            Assert(list.Count == 2, "Count should be 2.");
            Assert(list.SequenceEqual(new[] { 5, 15 }), "List should remain sorted.");
        });
    }

    void TestRemoveAt_FirstAndLast()
    {
        RunTest("RemoveAt - First and Last Elements", () =>
        {
            var list = new OrderList<int> { 5, 10, 15 };
            list.RemoveAt(2); // Remove last
            Assert(list.Count == 2, "Count should be 2 after removing last.");
            Assert(list.SequenceEqual(new[] { 5, 10 }), "Sequence should be correct.");
            list.RemoveAt(0); // Remove first
            Assert(list.Count == 1, "Count should be 1 after removing first.");
            Assert(list[0] == 10, "Remaining element should be correct.");
        });
    }

    // --- Access & Search Tests ---
    void TestIndexer_Get()
    {
        RunTest("Indexer - Get", () =>
        {
            var list = new OrderList<int> { 10, 20, 30 };
            Assert(list[1] == 20, "Indexer should return the correct item.");
            bool threw = false;
            try { var _ = list[3]; }
            catch (ArgumentOutOfRangeException) { threw = true; }
            Assert(threw, "Indexer should throw ArgumentOutOfRangeException for out-of-bounds access.");
        });
    }

    void TestIndexer_SetThrowsException()
    {
        RunTest("Indexer - Set Throws NotSupportedException", () =>
        {
            var list = new OrderList<int> { 10 };
            bool threw = false;
            try { list[0] = 5; }
            catch (NotSupportedException) { threw = true; }
            Assert(threw, "Setting via indexer should throw NotSupportedException.");
        });
    }

    void TestContains()
    {
        RunTest("Contains", () =>
        {
            var list = new OrderList<int> { 10, 20, 30 };
            Assert(list.Contains(20), "Contains should return true for an existing item.");
            Assert(!list.Contains(15), "Contains should return false for a non-existent item.");
        });
    }

    void TestIndexOf()
    {
        RunTest("IndexOf", () =>
        {
            var list = new OrderList<int> { 10, 20, 30 };
            Assert(list.IndexOf(20) == 1, "IndexOf should return the correct index for an existing item.");
            Assert(list.IndexOf(15) == -1, "IndexOf should return -1 for a non-existent item.");
        });
    }

    void TestAsSpan()
    {
        RunTest("AsSpan", () =>
        {
            var list = new OrderList<int> { 10, 20, 30 };
            Span<int> span = list.AsSpan();
            Assert(span.Length == 3, "Span length should match list count.");
            Assert(span[1] == 20, "Span content should be correct.");
        });
    }

    // --- Other ICollection/IList Methods ---
    void TestClear()
    {
        RunTest("Clear", () =>
        {
            var list = new OrderList<int> { 10, 20 };
            list.Clear();
            Assert(list.Count == 0, "Count should be 0 after Clear.");
        });
    }

    void TestCopyTo()
    {
        RunTest("CopyTo", () =>
        {
            var list = new OrderList<int> { 10, 20, 30 };
            var array = new int[3];
            list.CopyTo(array, 0);
            Assert(array.SequenceEqual(new[] { 10, 20, 30 }), "Array should contain the sorted elements of the list.");
        });
    }

    void TestEnumerator()
    {
        RunTest("Enumerator", () =>
        {
            var list = new OrderList<int> { 5, 1, 9 };
            var result = new List<int>();
            foreach (var item in list)
            {
                result.Add(item);
            }
            Assert(result.SequenceEqual(new[] { 1, 5, 9 }), "Enumerator should yield items in sorted order.");
        });
    }

    void TestInsert_ThrowsException()
    {
        RunTest("Insert - Throws NotSupportedException", () =>
        {
            var list = new OrderList<int> { 10 };
            bool threw = false;
            try { list.Insert(0, 5); }
            catch (NotSupportedException) { threw = true; }
            Assert(threw, "Insert should throw NotSupportedException.");
        });
    }

    #endregion
}