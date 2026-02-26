using System.Collections.Generic;
using System.Linq;
using UFlowFramework.DataStructure;
using UnityEngine;

namespace PowerCellStudio
{
    public class SortTest : RunTestMono
    {
        private class TestItem
        {
            public int Value { get; }
            public TestItem(int v) => Value = v;
            public override string ToString() => Value.ToString();
        }

        private readonly Sort.ValueMethod<TestItem> _valueMethod = item => item.Value;
        private readonly System.Random _random = new System.Random();

        void Start()
        {
            TestAllSorts();
        }

        private void TestAllSorts()
        {
            RunTest("BubbleSort", TestBubbleSort);
            RunTest("SelectionSort", TestSelectionSort);
            RunTest("InsertionSort", TestInsertionSort);
            RunTest("QuickSort", TestQuickSort);
            RunTest("HeapSort", TestHeapSort);
        }

        private List<TestItem> CreateRandomList(int count)
        {
            return Enumerable.Range(0, count).Select(i => new TestItem(_random.Next(0, 1000))).ToList();
        }

        private void AssertSorted(IList<TestItem> list, string messagePrefix)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                Assert(list[i].Value <= list[i + 1].Value, $"{messagePrefix}: List is not sorted at index {i}. {list[i].Value} > {list[i+1].Value}");
            }
        }
        
        private void AssertSubArraySorted(IList<TestItem> list, int start, int count, string messagePrefix)
        {
            for (int i = start; i < start + count - 1; i++)
            {
                Assert(list[i].Value <= list[i + 1].Value, $"{messagePrefix}: Sub-array is not sorted at index {i}.");
            }
        }

        private void TestSortAlgorithm(System.Action<IList<TestItem>, Sort.ValueMethod<TestItem>, int, int, int> sortAction, string sortName)
        {
            // Test with empty list
            var emptyList = new List<TestItem>();
            sortAction(emptyList, _valueMethod, 0, -1, -1);
            Assert(emptyList.Count == 0, $"{sortName} - Empty list");

            // Test with single element list
            var singleList = new List<TestItem> { new TestItem(5) };
            sortAction(singleList, _valueMethod, 0, -1, -1);
            Assert(singleList.Count == 1 && singleList[0].Value == 5, $"{sortName} - Single element list");

            // Test with random list
            var randomList = CreateRandomList(50);
            sortAction(randomList, _valueMethod, 0, -1, -1);
            AssertSorted(randomList, $"{sortName} - Random list");

            // Test with already sorted list
            var sortedList = Enumerable.Range(0, 50).Select(i => new TestItem(i)).ToList();
            sortAction(sortedList, _valueMethod, 0, -1, -1);
            AssertSorted(sortedList, $"{sortName} - Already sorted list");

            // Test with reverse sorted list
            var reverseList = Enumerable.Range(0, 50).Select(i => new TestItem(49 - i)).ToList();
            sortAction(reverseList, _valueMethod, 0, -1, -1);
            AssertSorted(reverseList, $"{sortName} - Reverse sorted list");
            
            // Test with startIndex and length
            var partialSortList = CreateRandomList(50);
            var originalPartial = partialSortList.Select(i => i.Value).ToArray();
            int startIndex = 10;
            int length = 20;
            sortAction(partialSortList, _valueMethod, startIndex, length, -1);
            AssertSubArraySorted(partialSortList, startIndex, length, $"{sortName} - Partial sort (startIndex, length)");
            for(int i = 0; i < startIndex; i++)
            {
                Assert(partialSortList[i].Value == originalPartial[i], $"{sortName} - Elements before startIndex should not change.");
            }
            for(int i = startIndex + length; i < partialSortList.Count; i++)
            {
                Assert(partialSortList[i].Value == originalPartial[i], $"{sortName} - Elements after sorted range should not change.");
            }

            // Test with takeCount (for BubbleSort, SelectionSort, HeapSort)
            if (sortName != "InsertionSort" && sortName != "QuickSort")
            {
                var takeCountList = CreateRandomList(50);
                int takeCount = 10;
                sortAction(takeCountList, _valueMethod, 0, -1, takeCount);
                // Only checks if the first 'takeCount' elements are the smallest, not necessarily sorted among themselves depending on the algorithm
                var sortedOriginal = takeCountList.OrderBy(x => x.Value).ToList();
                var tookElements = takeCountList.Take(takeCount).OrderBy(x => x.Value).ToList();
                for (int i = 0; i < takeCount; i++)
                {
                    Assert(tookElements[i].Value == sortedOriginal[i].Value, $"{sortName} - takeCount did not produce the smallest elements.");
                }
            }
        }

        private void TestBubbleSort()
        {
            TestSortAlgorithm(Sort.BubbleSort, "BubbleSort");
        }

        private void TestSelectionSort()
        {
            TestSortAlgorithm(Sort.SelectionSort, "SelectionSort");
        }

        private void TestInsertionSort()
        {
            TestSortAlgorithm((list, vm, start, len, take) => Sort.InsertionSort(list, vm, start, len), "InsertionSort");
        }

        private void TestQuickSort()
        {
            TestSortAlgorithm((list, vm, start, len, take) => Sort.QuickSort(list, vm, start, len), "QuickSort");
        }
        
        private void TestHeapSort()
        {
            TestSortAlgorithm(Sort.HeapSort, "HeapSort");
        }
    }
}
