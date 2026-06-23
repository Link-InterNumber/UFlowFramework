using System;
using System.Collections.Generic;
using System.Linq;
using UFlowFramework;
using UnityEngine;

public class MinHeapTest : PowerCellStudio.RunTestMono
{
    void OnEnable()
    {
        Debug.Log("========== MinHeap<T> Test Suite Started ==========");

        TestDefaultConstructor();
        TestConstructorWithCollection();
        TestAddAndPeek();
        TestAddMaintainsHeapStructure();
        TestPopAscendingOrder();
        TestTryPeekAndTryPop();
        TestAddRangeRebuildsHeap();
        TestRemoveItem();
        TestRemoveMaintainsHeapStructure();
        TestRemoveAt();
        TestIndexerSetFixesHeap();
        TestContainsAndIndexOf();
        TestCopyTo();
        TestClear();
        TestEnumerator();
        TestCapacityAndTrimExcess();

        Debug.Log("========== MinHeap<T> Test Suite Finished ==========");
    }

    void TestDefaultConstructor()
    {
        RunTest("MinHeap - Default Constructor", () =>
        {
            var heap = new MinHeap<int>();
            Assert(heap.Count == 0, "Default constructor should create an empty heap.");
            Assert(!heap.IsReadOnly, "Heap should not be read-only.");
        });
    }

    void TestConstructorWithCollection()
    {
        RunTest("MinHeap - Constructor With Collection", () =>
        {
            var heap = new MinHeap<int>(new[] { 5, 1, 9, 3, 2 });
            Assert(heap.Count == 5, "Count should match source collection count.");
            Assert(heap.Peek() == 1, "Min heap root should be the smallest item after construction.");
            AssertPopOrder(heap, new[] { 1, 2, 3, 5, 9 });
        });
    }

    void TestAddAndPeek()
    {
        RunTest("MinHeap - Add and Peek", () =>
        {
            var heap = new MinHeap<int>();
            heap.Add(10);
            heap.Add(3);
            heap.Add(7);
            heap.Add(1);

            Assert(heap.Count == 4, "Count should be 4 after adding four items.");
            Assert(heap.Peek() == 1, "Peek should return the smallest item.");
        });
    }

    void TestAddMaintainsHeapStructure()
    {
        RunTest("MinHeap - Add Maintains Heap Structure", () =>
        {
            var heap = new MinHeap<int>();
            var values = new[] { 10, 3, 7, 20, 1, 15, 30, 2, 25 };

            for (var i = 0; i < values.Length; i++)
            {
                heap.Add(values[i]);
                Assert(IsValidMinHeap(heap), $"Heap structure should be valid after adding {values[i]} at step {i}.");
            }

            AssertPopOrder(heap, new[] { 1, 2, 3, 7, 10, 15, 20, 25, 30 });
        });
    }

    void TestPopAscendingOrder()
    {
        RunTest("MinHeap - Pop Ascending Order", () =>
        {
            var heap = new MinHeap<int>(new[] { 4, 1, 7, 1, 9, 2, 6 });
            AssertPopOrder(heap, new[] { 1, 1, 2, 4, 6, 7, 9 });
            Assert(heap.Count == 0, "Heap should be empty after popping all items.");
        });
    }

    void TestTryPeekAndTryPop()
    {
        RunTest("MinHeap - TryPeek and TryPop", () =>
        {
            var heap = new MinHeap<int>();
            Assert(!heap.TryPeek(out _), "TryPeek should return false for empty heap.");
            Assert(!heap.TryPop(out _), "TryPop should return false for empty heap.");

            heap.Add(8);
            heap.Add(2);
            Assert(heap.TryPeek(out var peeked) && peeked == 2, "TryPeek should return current minimum.");
            Assert(heap.TryPop(out var popped) && popped == 2, "TryPop should remove current minimum.");
            Assert(heap.Count == 1 && heap.Peek() == 8, "Remaining item should be 8.");
        });
    }

    void TestAddRangeRebuildsHeap()
    {
        RunTest("MinHeap - AddRange Rebuilds Heap", () =>
        {
            var heap = new MinHeap<int>();
            heap.Add(50);
            heap.AddRange(new[] { 20, 5, 30, 1, 40 });

            Assert(heap.Peek() == 1, "AddRange should rebuild heap and put smallest item at root.");
            AssertPopOrder(heap, new[] { 1, 5, 20, 30, 40, 50 });
        });
    }

    void TestRemoveItem()
    {
        RunTest("MinHeap - Remove Item", () =>
        {
            var heap = new MinHeap<int>(new[] { 9, 1, 5, 3, 7 });
            Assert(heap.Remove(3), "Remove should return true for existing item.");
            Assert(!heap.Contains(3), "Heap should not contain removed item.");
            Assert(heap.Peek() == 1, "Heap root should remain valid after remove.");
            AssertPopOrder(heap, new[] { 1, 5, 7, 9 });
            Assert(!heap.Remove(100), "Remove should return false for non-existent item.");
        });
    }

    void TestRemoveMaintainsHeapStructure()
    {
        RunTest("MinHeap - Remove Maintains Heap Structure", () =>
        {
            var heap = new MinHeap<int>(new[] { 50, 20, 40, 10, 30, 35, 45, 5, 15, 25 });
            Assert(IsValidMinHeap(heap), "Initial heap structure should be valid.");

            var valuesToRemove = new[] { 20, 45, 5, 50, 30 };
            for (var i = 0; i < valuesToRemove.Length; i++)
            {
                Assert(heap.Remove(valuesToRemove[i]), $"Remove should return true for {valuesToRemove[i]}.");
                Assert(IsValidMinHeap(heap), $"Heap structure should be valid after removing {valuesToRemove[i]} at step {i}.");
            }

            AssertPopOrder(heap, new[] { 10, 15, 25, 35, 40 });
        });
    }

    void TestRemoveAt()
    {
        RunTest("MinHeap - RemoveAt", () =>
        {
            var heap = new MinHeap<int>(new[] { 8, 4, 6, 2, 10, 1 });
            var removed = heap[0];
            heap.RemoveAt(0);

            Assert(removed == 1, "Index 0 should be the minimum before RemoveAt.");
            Assert(heap.Peek() == 2, "Heap root should be repaired after removing root by index.");
            AssertPopOrder(heap, new[] { 2, 4, 6, 8, 10 });
        });
    }

    void TestIndexerSetFixesHeap()
    {
        RunTest("MinHeap - Indexer Set Fixes Heap", () =>
        {
            var heap = new MinHeap<int>(new[] { 10, 20, 30, 40 });
            heap[2] = 1;

            Assert(heap.Peek() == 1, "Setting a smaller value should sift it up to root.");
            AssertPopOrder(heap, new[] { 1, 10, 20, 40 });
        });
    }

    void TestContainsAndIndexOf()
    {
        RunTest("MinHeap - Contains and IndexOf", () =>
        {
            var heap = new MinHeap<int>(new[] { 4, 2, 8 });
            Assert(heap.Contains(2), "Contains should find existing item.");
            Assert(!heap.Contains(7), "Contains should return false for non-existent item.");
            Assert(heap.IndexOf(8) >= 0, "IndexOf should return valid index for existing item.");
            Assert(heap.IndexOf(7) == -1, "IndexOf should return -1 for non-existent item.");
        });
    }

    void TestCopyTo()
    {
        RunTest("MinHeap - CopyTo", () =>
        {
            var heap = new MinHeap<int>(new[] { 3, 1, 2 });
            var array = new int[5];
            heap.CopyTo(array, 1);

            Assert(array[0] == 0, "CopyTo should respect destination index.");
            Assert(array.Skip(1).Take(heap.Count).OrderBy(v => v).SequenceEqual(new[] { 1, 2, 3 }), "Copied items should match heap contents.");
        });
    }

    void TestClear()
    {
        RunTest("MinHeap - Clear", () =>
        {
            var heap = new MinHeap<int>(new[] { 3, 1, 2 });
            heap.Clear();
            Assert(heap.Count == 0, "Count should be 0 after Clear.");
            Assert(!heap.TryPeek(out _), "TryPeek should fail after Clear.");
        });
    }

    void TestEnumerator()
    {
        RunTest("MinHeap - Enumerator", () =>
        {
            var heap = new MinHeap<int>(new[] { 5, 1, 3 });
            var values = new List<int>();
            foreach (var item in heap)
            {
                values.Add(item);
            }

            Assert(values.Count == 3, "Enumerator should yield all items.");
            Assert(values.OrderBy(v => v).SequenceEqual(new[] { 1, 3, 5 }), "Enumerator should contain all heap items.");
        });
    }

    void TestCapacityAndTrimExcess()
    {
        RunTest("MinHeap - Capacity and TrimExcess", () =>
        {
            var heap = new MinHeap<int>(16);
            heap.AddRange(new[] { 3, 1, 2 });
            Assert(heap.Capacity >= 3, "Capacity should be at least count.");

            heap.TrimExcess();
            Assert(heap.Capacity >= heap.Count, "Capacity should still be at least count after TrimExcess.");
        });
    }

    private void AssertPopOrder(MinHeap<int> heap, int[] expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            var value = heap.Pop();
            Assert(value == expected[i], $"Pop order mismatch at {i}. Expected {expected[i]}, got {value}.");
        }
    }

    private bool IsValidMinHeap(MinHeap<int> heap)
    {
        for (var i = 0; i < heap.Count; i++)
        {
            var leftIndex = i * 2 + 1;
            var rightIndex = leftIndex + 1;

            if (leftIndex < heap.Count && heap[i] > heap[leftIndex])
            {
                return false;
            }

            if (rightIndex < heap.Count && heap[i] > heap[rightIndex])
            {
                return false;
            }
        }

        return true;
    }
}
