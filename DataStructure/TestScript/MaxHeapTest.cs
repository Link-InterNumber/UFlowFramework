using System;
using System.Collections.Generic;
using System.Linq;
using UFlowFramework;
using UnityEngine;

public class MaxHeapTest : PowerCellStudio.RunTestMono
{
    void OnEnable()
    {
        Debug.Log("========== MaxHeap<T> Test Suite Started ==========");

        TestDefaultConstructor();
        TestConstructorWithCollection();
        TestAddAndPeek();
        TestAddMaintainsHeapStructure();
        TestPopDescendingOrder();
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

        Debug.Log("========== MaxHeap<T> Test Suite Finished ==========");
    }

    void TestDefaultConstructor()
    {
        RunTest("MaxHeap - Default Constructor", () =>
        {
            var heap = new MaxHeap<int>();
            Assert(heap.Count == 0, "Default constructor should create an empty heap.");
            Assert(!heap.IsReadOnly, "Heap should not be read-only.");
        });
    }

    void TestConstructorWithCollection()
    {
        RunTest("MaxHeap - Constructor With Collection", () =>
        {
            var heap = new MaxHeap<int>(new[] { 5, 1, 9, 3, 2 });
            Assert(heap.Count == 5, "Count should match source collection count.");
            Assert(heap.Peek() == 9, "Max heap root should be the largest item after construction.");
            AssertPopOrder(heap, new[] { 9, 5, 3, 2, 1 });
        });
    }

    void TestAddAndPeek()
    {
        RunTest("MaxHeap - Add and Peek", () =>
        {
            var heap = new MaxHeap<int>();
            heap.Add(10);
            heap.Add(3);
            heap.Add(7);
            heap.Add(20);

            Assert(heap.Count == 4, "Count should be 4 after adding four items.");
            Assert(heap.Peek() == 20, "Peek should return the largest item.");
        });
    }

    void TestAddMaintainsHeapStructure()
    {
        RunTest("MaxHeap - Add Maintains Heap Structure", () =>
        {
            var heap = new MaxHeap<int>();
            var values = new[] { 10, 3, 7, 20, 1, 15, 30, 2, 25 };

            for (var i = 0; i < values.Length; i++)
            {
                heap.Add(values[i]);
                Assert(IsValidMaxHeap(heap), $"Heap structure should be valid after adding {values[i]} at step {i}.");
            }

            AssertPopOrder(heap, new[] { 30, 25, 20, 15, 10, 7, 3, 2, 1 });
        });
    }

    void TestPopDescendingOrder()
    {
        RunTest("MaxHeap - Pop Descending Order", () =>
        {
            var heap = new MaxHeap<int>(new[] { 4, 1, 7, 1, 9, 2, 6 });
            AssertPopOrder(heap, new[] { 9, 7, 6, 4, 2, 1, 1 });
            Assert(heap.Count == 0, "Heap should be empty after popping all items.");
        });
    }

    void TestTryPeekAndTryPop()
    {
        RunTest("MaxHeap - TryPeek and TryPop", () =>
        {
            var heap = new MaxHeap<int>();
            Assert(!heap.TryPeek(out _), "TryPeek should return false for empty heap.");
            Assert(!heap.TryPop(out _), "TryPop should return false for empty heap.");

            heap.Add(8);
            heap.Add(20);
            Assert(heap.TryPeek(out var peeked) && peeked == 20, "TryPeek should return current maximum.");
            Assert(heap.TryPop(out var popped) && popped == 20, "TryPop should remove current maximum.");
            Assert(heap.Count == 1 && heap.Peek() == 8, "Remaining item should be 8.");
        });
    }

    void TestAddRangeRebuildsHeap()
    {
        RunTest("MaxHeap - AddRange Rebuilds Heap", () =>
        {
            var heap = new MaxHeap<int>();
            heap.Add(1);
            heap.AddRange(new[] { 20, 5, 30, 50, 40 });

            Assert(heap.Peek() == 50, "AddRange should rebuild heap and put largest item at root.");
            AssertPopOrder(heap, new[] { 50, 40, 30, 20, 5, 1 });
        });
    }

    void TestRemoveItem()
    {
        RunTest("MaxHeap - Remove Item", () =>
        {
            var heap = new MaxHeap<int>(new[] { 9, 1, 5, 3, 7 });
            Assert(heap.Remove(3), "Remove should return true for existing item.");
            Assert(!heap.Contains(3), "Heap should not contain removed item.");
            Assert(heap.Peek() == 9, "Heap root should remain valid after remove.");
            AssertPopOrder(heap, new[] { 9, 7, 5, 1 });
            Assert(!heap.Remove(100), "Remove should return false for non-existent item.");
        });
    }

    void TestRemoveMaintainsHeapStructure()
    {
        RunTest("MaxHeap - Remove Maintains Heap Structure", () =>
        {
            var heap = new MaxHeap<int>(new[] { 50, 20, 40, 10, 30, 35, 45, 5, 15, 25 });
            Assert(IsValidMaxHeap(heap), "Initial heap structure should be valid.");

            var valuesToRemove = new[] { 20, 45, 50, 5, 30 };
            for (var i = 0; i < valuesToRemove.Length; i++)
            {
                Assert(heap.Remove(valuesToRemove[i]), $"Remove should return true for {valuesToRemove[i]}.");
                Assert(IsValidMaxHeap(heap), $"Heap structure should be valid after removing {valuesToRemove[i]} at step {i}.");
            }

            AssertPopOrder(heap, new[] { 40, 35, 25, 15, 10 });
        });
    }

    void TestRemoveAt()
    {
        RunTest("MaxHeap - RemoveAt", () =>
        {
            var heap = new MaxHeap<int>(new[] { 8, 4, 6, 2, 10, 1 });
            var removed = heap[0];
            heap.RemoveAt(0);

            Assert(removed == 10, "Index 0 should be the maximum before RemoveAt.");
            Assert(heap.Peek() == 8, "Heap root should be repaired after removing root by index.");
            AssertPopOrder(heap, new[] { 8, 6, 4, 2, 1 });
        });
    }

    void TestIndexerSetFixesHeap()
    {
        RunTest("MaxHeap - Indexer Set Fixes Heap", () =>
        {
            var heap = new MaxHeap<int>(new[] { 10, 20, 30, 40 });
            heap[2] = 100;

            Assert(heap.Peek() == 100, "Setting a larger value should sift it up to root.");
            AssertPopOrder(heap, new[] { 100, 40, 20, 10 });
        });
    }

    void TestContainsAndIndexOf()
    {
        RunTest("MaxHeap - Contains and IndexOf", () =>
        {
            var heap = new MaxHeap<int>(new[] { 4, 2, 8 });
            Assert(heap.Contains(2), "Contains should find existing item.");
            Assert(!heap.Contains(7), "Contains should return false for non-existent item.");
            Assert(heap.IndexOf(8) >= 0, "IndexOf should return valid index for existing item.");
            Assert(heap.IndexOf(7) == -1, "IndexOf should return -1 for non-existent item.");
        });
    }

    void TestCopyTo()
    {
        RunTest("MaxHeap - CopyTo", () =>
        {
            var heap = new MaxHeap<int>(new[] { 3, 1, 2 });
            var array = new int[5];
            heap.CopyTo(array, 1);

            Assert(array[0] == 0, "CopyTo should respect destination index.");
            Assert(array.Skip(1).Take(heap.Count).OrderBy(v => v).SequenceEqual(new[] { 1, 2, 3 }), "Copied items should match heap contents.");
        });
    }

    void TestClear()
    {
        RunTest("MaxHeap - Clear", () =>
        {
            var heap = new MaxHeap<int>(new[] { 3, 1, 2 });
            heap.Clear();
            Assert(heap.Count == 0, "Count should be 0 after Clear.");
            Assert(!heap.TryPeek(out _), "TryPeek should fail after Clear.");
        });
    }

    void TestEnumerator()
    {
        RunTest("MaxHeap - Enumerator", () =>
        {
            var heap = new MaxHeap<int>(new[] { 5, 1, 3 });
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
        RunTest("MaxHeap - Capacity and TrimExcess", () =>
        {
            var heap = new MaxHeap<int>(16);
            heap.AddRange(new[] { 3, 1, 2 });
            Assert(heap.Capacity >= 3, "Capacity should be at least count.");

            heap.TrimExcess();
            Assert(heap.Capacity >= heap.Count, "Capacity should still be at least count after TrimExcess.");
        });
    }

    private void AssertPopOrder(MaxHeap<int> heap, int[] expected)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            var value = heap.Pop();
            Assert(value == expected[i], $"Pop order mismatch at {i}. Expected {expected[i]}, got {value}.");
        }
    }

    private bool IsValidMaxHeap(MaxHeap<int> heap)
    {
        for (var i = 0; i < heap.Count; i++)
        {
            var leftIndex = i * 2 + 1;
            var rightIndex = leftIndex + 1;

            if (leftIndex < heap.Count && heap[i] < heap[leftIndex])
            {
                return false;
            }

            if (rightIndex < heap.Count && heap[i] < heap[rightIndex])
            {
                return false;
            }
        }

        return true;
    }
}
