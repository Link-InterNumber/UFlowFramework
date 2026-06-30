using System.Collections.Generic;
using System.Linq;
using PowerCellStudio;
using UFlowFramework;
using UnityEngine;

public class OrderListMinHeapPerformanceTest : RunTestMono
{
    public int InitialItems = 100000;
    public int DynamicAddOperations = 20000;
    public int DynamicRemoveOperations = 20000;

    void OnEnable()
    {
        Debug.Log($"========== OrderList vs MinHeap Performance Test Started (Initial: {InitialItems}, Add: {DynamicAddOperations}, Remove: {DynamicRemoveOperations}) ==========");

        var random = new System.Random(20260624);
        var initialItems = CreateRandomItems(random, InitialItems);
        var addItems = CreateRandomItems(random, DynamicAddOperations);
        var removeItems = initialItems.OrderBy(_ => random.Next()).Take(DynamicRemoveOperations).ToArray();

        RunBuildTests(initialItems);
        RunDynamicAddTests(initialItems, addItems);
        RunDynamicRemoveTests(initialItems, removeItems);
        RunMixedDynamicTests(initialItems, addItems, removeItems);

        Debug.Log("========== OrderList vs MinHeap Performance Test Finished ==========");
    }

    private void RunBuildTests(int[] initialItems)
    {
        Debug.Log("--- Build Performance ---");

        RunPerformanceTest("OrderList - Build by Add one by one", () =>
        {
            var orderList = new OrderList<int>(initialItems.Length);
            foreach (var item in initialItems)
            {
                orderList.Add(item);
            }
            Consume(orderList.Count);
        });

        RunPerformanceTest("OrderList - Build by constructor", () =>
        {
            var orderList = new OrderList<int>(initialItems);
            Consume(orderList.Count);
        });

        RunPerformanceTest("MinHeap - Build by Add one by one", () =>
        {
            var minHeap = new MinHeap<int>(initialItems.Length);
            foreach (var item in initialItems)
            {
                minHeap.Add(item);
            }
            Consume(minHeap.Count);
        });

        RunPerformanceTest("MinHeap - Build by constructor Heapify", () =>
        {
            var minHeap = new MinHeap<int>(initialItems);
            Consume(minHeap.Count);
        });
    }

    private void RunDynamicAddTests(int[] initialItems, int[] addItems)
    {
        Debug.Log("--- Dynamic Random Add Performance ---");

        RunPerformanceTest("OrderList - Dynamic Add", () =>
        {
            var orderList = new OrderList<int>(initialItems);
            long checksum = 0;
            foreach (var item in addItems)
            {
                orderList.Add(item);
                checksum += orderList[0];
            }
            Consume(checksum);
        });

        RunPerformanceTest("MinHeap - Dynamic Add", () =>
        {
            var minHeap = new MinHeap<int>(initialItems);
            long checksum = 0;
            foreach (var item in addItems)
            {
                minHeap.Add(item);
                checksum += minHeap.Peek();
            }
            Consume(checksum);
        });
    }

    private void RunDynamicRemoveTests(int[] initialItems, int[] removeItems)
    {
        Debug.Log("--- Dynamic Random Remove Performance ---");

        RunPerformanceTest("OrderList - Dynamic Remove random values", () =>
        {
            var orderList = new OrderList<int>(initialItems);
            var removed = 0;
            long checksum = 0;
            foreach (var item in removeItems)
            {
                if (orderList.Remove(item))
                {
                    removed++;
                    if (orderList.Count > 0)
                    {
                        checksum += orderList[0];
                    }
                }
            }
            Consume(checksum + removed);
        });

        RunPerformanceTest("MinHeap - Dynamic Remove random values", () =>
        {
            var minHeap = new MinHeap<int>(initialItems);
            var removed = 0;
            long checksum = 0;
            foreach (var item in removeItems)
            {
                if (minHeap.Remove(item))
                {
                    removed++;
                    if (minHeap.Count > 0)
                    {
                        checksum += minHeap.Peek();
                    }
                }
            }
            Consume(checksum + removed);
        });
    }

    private void RunMixedDynamicTests(int[] initialItems, int[] addItems, int[] removeItems)
    {
        Debug.Log("--- Mixed Dynamic Random Add/Remove Performance ---");

        var operations = BuildMixedOperations(addItems, removeItems);

        RunPerformanceTest("OrderList - Mixed Add/Remove", () =>
        {
            var orderList = new OrderList<int>(initialItems);
            long checksum = 0;
            foreach (var operation in operations)
            {
                if (operation.IsAdd)
                {
                    orderList.Add(operation.Value);
                }
                else
                {
                    orderList.Remove(operation.Value);
                }

                if (orderList.Count > 0)
                {
                    checksum += orderList[0];
                }
            }
            Consume(checksum);
        });

        RunPerformanceTest("MinHeap - Mixed Add/Remove", () =>
        {
            var minHeap = new MinHeap<int>(initialItems);
            long checksum = 0;
            foreach (var operation in operations)
            {
                if (operation.IsAdd)
                {
                    minHeap.Add(operation.Value);
                }
                else
                {
                    minHeap.Remove(operation.Value);
                }

                if (minHeap.Count > 0)
                {
                    checksum += minHeap.Peek();
                }
            }
            Consume(checksum);
        });
    }

    private static int[] CreateRandomItems(System.Random random, int count)
    {
        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = random.Next();
        }
        return result;
    }

    private static HeapOperation[] BuildMixedOperations(int[] addItems, int[] removeItems)
    {
        var count = addItems.Length + removeItems.Length;
        var result = new HeapOperation[count];
        var writeIndex = 0;
        for (var i = 0; i < addItems.Length; i++)
        {
            result[writeIndex++] = new HeapOperation(true, addItems[i]);
        }
        for (var i = 0; i < removeItems.Length; i++)
        {
            result[writeIndex++] = new HeapOperation(false, removeItems[i]);
        }

        var random = new System.Random(2026062401);
        for (var i = result.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            var temp = result[i];
            result[i] = result[j];
            result[j] = temp;
        }

        return result;
    }

    private static void Consume<T>(T value)
    {
        if (value == null)
        {
            Debug.Log(string.Empty);
        }
    }

    private readonly struct HeapOperation
    {
        public readonly bool IsAdd;
        public readonly int Value;

        public HeapOperation(bool isAdd, int value)
        {
            IsAdd = isAdd;
            Value = value;
        }
    }
}
