using System.Collections.Generic;
using System.Linq;
using UFlowFramework;
using UnityEngine;

public class MaxHeapPerformanceTest : PowerCellStudio.RunTestMono
{
    public int NUM_ITEMS = 100000;
    public int FIND_OPERATIONS = 10000;
    public int DynamicOperations = 1000;

    void OnEnable()
    {
        Debug.Log($"========== MaxHeap Max-Value Performance Test Started (Items: {NUM_ITEMS}, Finds: {FIND_OPERATIONS}) ==========");

        var random = new System.Random(20260623);
        var items = new int[NUM_ITEMS];
        for (var i = 0; i < NUM_ITEMS; i++)
        {
            items[i] = random.Next();
        }

        RunAllTests(items, random);

        Debug.Log("========== MaxHeap Max-Value Performance Test Finished ==========");
    }

    private void RunAllTests(int[] items, System.Random random)
    {
        Debug.Log("--- Build Cost ---");

        MaxHeap<int> maxHeap = null;
        RunPerformanceTest("MaxHeap - Build by Heapify", () =>
        {
            maxHeap = new MaxHeap<int>(items);
        });

        List<int> sortedList = null;
        RunPerformanceTest("List.Sort + Reverse - Build sorted list", () =>
        {
            sortedList = new List<int>(items);
            sortedList.Sort();
            sortedList.Reverse();
        });

        List<int> linqOrderedList = null;
        RunPerformanceTest("LINQ OrderByDescending - Build sorted list", () =>
        {
            linqOrderedList = items.OrderByDescending(v => v).ToList();
        });

        Debug.Log("--- Find Max Cost ---");

        RunPerformanceTest("MaxHeap.Peek - Find max repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += maxHeap.Peek();
            }
            Consume(checksum);
        });

        RunPerformanceTest("List.Sort + Reverse + List[0] - Find max repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += sortedList[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("LINQ OrderByDescending + List[0] - Find max repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += linqOrderedList[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("LINQ Max - Find max repeatedly", () =>
        {
            long checksum = 0;
            for (var i = 0; i < FIND_OPERATIONS; i++)
            {
                checksum += items.Max();
            }
            Consume(checksum);
        });

        
        Debug.Log("--- Dynamic Operations ---");

        RunPerformanceTest("MaxHeap - Add + Remove", () =>
        {
            var heap = new MaxHeap<int>(items);
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

        RunPerformanceTest("List - Add + Remove + Sort + Reverse", () =>
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
                list.Reverse();
                checksum += list[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("LINQ - Add + Remove + OrderByDescending", () =>
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
                var orderedList = list.OrderByDescending(v => v).ToList();
                checksum += orderedList[0];
            }
            Consume(checksum);
        });


        Debug.Log("--- Build + Find Once Cost ---");

        RunPerformanceTest("MaxHeap - Build + Peek once", () =>
        {
            var heap = new MaxHeap<int>(items);
            Consume(heap.Peek());
        });

        RunPerformanceTest("List.Sort + Reverse + List[0] once", () =>
        {
            var list = new List<int>(items);
            list.Sort();
            list.Reverse();
            Consume(list[0]);
        });

        RunPerformanceTest("LINQ OrderByDescending + First once", () =>
        {
            Consume(items.OrderByDescending(v => v).First());
        });

        RunPerformanceTest("LINQ Max once", () =>
        {
            Consume(items.Max());
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
