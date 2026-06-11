using System;
using System.Collections.Generic;
using UnityEngine;

namespace UFlowFramework.DataStructure
{
    public static partial class Sort
    {
        public delegate int ValueMethod<T>(T a);

        private static bool NeedSort<T>(IList<T> list, ValueMethod<T> valueMethod)
        {
            if (list == null)
            {
                Debug.LogError("list is null");
                return false;
            }

            if (valueMethod == null)
            {
                Debug.LogError("valueMethod is null");
                return false;
            }

            int n = list.Count;
            if (n < 2) return false;
            return true;
        }

        private static bool NeedSort<T>(IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
            {
                Debug.LogError("list is null");
                return false;
            }

            if (comparison == null)
            {
                Debug.LogError("comparison is null");
                return false;
            }

            int n = list.Count;
            if (n < 2) return false;
            return true;
        }

        private static bool NeedSort<T>(Span<T> span, ValueMethod<T> valueMethod)
        {
            if (valueMethod == null)
            {
                Debug.LogError("valueMethod is null");
                return false;
            }

            int n = span.Length;
            if (n < 2) return false;
            return true;
        }

        private static bool NeedSort<T>(Span<T> span, Comparison<T> comparison)
        {
            if (comparison == null)
            {
                Debug.LogError("comparison is null");
                return false;
            }

            int n = span.Length;
            if (n < 2) return false;
            return true;
        }

        private static bool NeedSort<T>(Span<T> span)
            where T : IComparable<T>
        {
            int n = span.Length;
            if (n < 2) return false;
            return true;
        }

        private static bool NeedSort<T>(IList<T> list)
            where T : IComparable<T>
        {
            if (list == null)
            {
                Debug.LogError("list is null");
                return false;
            }

            int n = list.Count;
            if (n < 2) return false;
            return true;
        }

        private static void Swap<T>(IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        private static void Swap<T>(Span<T> span, int i, int j)
        {
            var temp = span[i];
            span[i] = span[j];
            span[j] = temp;
        }

        private static bool CheckParameters<T>(IList<T> list, int startIndex, ref int length)
        {
            if (startIndex < 0 || startIndex >= list.Count)
            {
                Debug.LogError("startIndex is out of range");
                return false;
            }

            if (length < 0)
            {
                length = list.Count - startIndex;
            }
            else
            {
                length = Math.Min(length, list.Count - startIndex);
            }

            if (length == 0)
            {
                Debug.LogError("length is 0");
                return false;
            }

            return true;
        }

        private static bool CheckParameters<T>(Span<T> span, int startIndex, ref int length)
        {
            if (startIndex < 0 || startIndex >= span.Length)
            {
                Debug.LogError("startIndex is out of range");
                return false;
            }

            if (length < 0)
            {
                length = span.Length - startIndex;
            }
            else
            {
                length = Math.Min(length, span.Length - startIndex);
            }

            if (length == 0)
            {
                Debug.LogError("length is 0");
                return false;
            }

            return true;
        }
    }
}