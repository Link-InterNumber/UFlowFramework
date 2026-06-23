using System.Collections.Generic;
using System.Linq;
using UFlowFramework;
using UnityEngine;

public class MinHeapPerformanceTest : PowerCellStudio.RunTestMono
{
    public int NUM_ITEMS = 100000;
    public int FIND_OPERATIONS = 10000;
    public int DynamicOperations = 1000;

    void OnEnable()
    {
        Debug.Log($"========== MinHeap Min-Value Performance Test Started (Items: {NUM_ITEMS}, Finds: {FIND_OPERATIONS}) ==========");

        var random = new System.Random(20260623);
        var items = new int[NUM_ITEMS];
        for (var i = 0; i < NUM_ITEMS; i++)
        {
            items[i] = random.Next();
        }

        RunAllTests(items, random);

        Debug.Log("========== MinHeap Min-Value Performance Test Finished ==========");
    }

    private void RunAllTests(int[] items, System.Random random)
    {
        Debug.Log("--- Build Cost ---");

        MinHeap<int> minHeap = null;
        RunPerformanceTest("MinHeap - Build by Heapify", () =>
        {
            minHeap = new MinHeap<int>(items);
        });

        List<int> sortedList = null;
        RunPerformanceTest("List.Sort - Build sorted list", () =>
        {
            sortedList = new List<int>(items);
            sortedList.Sort();
        });

        List<int> linqOrderedList = null;
        RunPerformanceTest("LINQ OrderBy - Build sorted list", () =>
        {
            linqOrderedList = items.OrderBy(v => v).ToList();
        });

        Debug.Log("--- Find Min Cost ---");

        RunPerformanceTest("MinHeap.Peek - Find min repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += minHeap.Peek();
            }
            Consume(checksum);
        });

        RunPerformanceTest("List.Sort + List[0] - Find min repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += sortedList[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("LINQ OrderBy + List[0] - Find min repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += linqOrderedList[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("LINQ Min - Find min repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += items.Min();
            }
            Consume(checksum);
        });

        Debug.Log("--- Dynamic Operations ---");

        RunPerformanceTest("MinHeap - Add + Remove", () =>
        {
            var heap = new MinHeap<int>(items);
            long checksum = 0;
            for (var i = 0; i < DynamicOperations; i++)
            {
                var isAddOperation = random.NextDouble() < 0.5;
                if (isAddOperation)
                {
                    heap.Add(random.Next());
                }
                else
                {
                    heap.Pop();
                }
                checksum += heap.Peek();
            }
            Consume(checksum);
        });

        RunPerformanceTest("List - Add + Remove + Sort", () =>
        {
            var list = new List<int>(items);
            long checksum = 0;
            for (var i = 0; i < DynamicOperations; i++)
            {
                var isAddOperation = random.NextDouble() < 0.5;
                if (isAddOperation)
                {
                    list.Add(random.Next());
                }
                else
                {
                    list.RemoveAt(0);
                }
                list.Sort();
                checksum += list[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("LINQ - Add + Remove + OrderBy", () =>
        {
            var list = new List<int>(items);
            long checksum = 0;
            for (var i = 0; i < DynamicOperations; i++)
            {
                var isAddOperation = random.NextDouble() < 0.5;
                if (isAddOperation)
                {
                    list.Add(random.Next());
                }
                else
                {
                    list.RemoveAt(0);
                }
                var orderedList = list.OrderBy(v => v).ToList();
                checksum += orderedList[0];
            }
            Consume(checksum);
        });

        Debug.Log("--- Build + Find Once Cost ---");

        RunPerformanceTest("MinHeap - Build + Peek once", () =>
        {
            var heap = new MinHeap<int>(items);
            Consume(heap.Peek());
        });

        RunPerformanceTest("List.Sort + List[0] once", () =>
        {
            var list = new List<int>(items);
            list.Sort();
            Consume(list[0]);
        });

        RunPerformanceTest("LINQ OrderBy + First once", () =>
        {
            Consume(items.OrderBy(v => v).First());
        });

        RunPerformanceTest("LINQ Min once", () =>
        {
            Consume(items.Min());
        });
    }

    private static void Consume<T>(T value)
    {
        if (value == null)
        {
            Debug.Log(string.Empty);
        }
    }
}
