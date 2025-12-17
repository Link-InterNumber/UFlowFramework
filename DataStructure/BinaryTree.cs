using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public interface IDelta<T>
    {
        public int DeltaTo(T other);
    }

    public class BinaryTree<T> : IEnumerable<T> where T : IComparable<T> , IDelta<T>
    {
        private class BinaryTreeNode<K> where K : IComparable<K> , IDelta<K>
        {
            public int deepLv;
            public K valueT;

            public BinaryTreeNode<K> left;

            public BinaryTreeNode<K> right;

            public BinaryTreeNode(K v, int deepLv)
            {
                valueT = v;
                this.deepLv = deepLv;
            }
        }

        private BinaryTreeNode<T> _root;
        private HashSet<T> _objects;
        private List<T> _rawData;

        private int _deep;
        private int _nodeCount;

        public int Count => _nodeCount;

        public BinaryTree()
        {
            _rawData = new List<T>();
            _objects = new HashSet<T>();
        }

        public void Clear()
        {
            _rawData.Clear();
            _root = null;
            _objects.Clear();
            _nodeCount= 0;
            _deep = 0;
            _isBuilt = false;
        }

        public void Insert(T obj)
        {
            if (obj == null || _objects.Contains(obj)) return;
            _objects.Add(obj);
            _rawData.Add(obj);
            _nodeCount++;
            if (_isBuilt)
            {
                Build();
            }
        }

        public bool Remove(T obj)
        {
            if (obj == null || !_objects.Contains(obj)) return false;
            _objects.Remove(obj);
            var success = _rawData.Remove(obj);
            if (success)
            {
                _nodeCount--;
            }
            if (_isBuilt)
            {
                Build();
            }
            return success;
        }

        private bool _isBuilt = false;
        public void Build()
        {
            if (_rawData.Count == 0)
            {
                _root = null;
                _deep = 0;
                _isBuilt = true;
                return;
            }
            _isBuilt = true;
            _rawData.Sort();
            var centerIndex = (int)Math.Floor((_rawData.Count - 1) / 2f);
            _deep = 1;
            _root = new BinaryTreeNode<T>(_rawData[centerIndex], _deep);
            _root.left = BuildHandler(_root, 0, centerIndex);
            _root.right = BuildHandler(_root, centerIndex + 1, _rawData.Count - centerIndex - 1);
        }

        private BinaryTreeNode<T> BuildHandler(BinaryTreeNode<T> root, int startIndex, int subListLength)
        {
            if (subListLength < 1) return null;
            var deep = root.deepLv + 1;
            if (subListLength == 1) return new BinaryTreeNode<T>(_rawData[startIndex], deep);
            var centerIndex = (int)Math.Floor((subListLength - 1) / 2f) + startIndex;
            var newNode = new BinaryTreeNode<T>(_rawData[centerIndex], deep);
            newNode.left = BuildHandler(newNode, startIndex, centerIndex - startIndex);
            newNode.right = BuildHandler(newNode, centerIndex + 1, startIndex + subListLength - centerIndex - 1);
            return newNode;
        }

        private int GetIndex(T obj)
        {
            if (_isBuilt)
            {
                return _rawData.BinarySearch(obj);
            }
            else
            {
                return _rawData.IndexOf(obj);
            }
        }

        // 寻找最近似值
        public T Find(T obj)
        {
            var node = _root;
            var closest = node;
            var closestDelta = obj.DeltaTo(closest.valueT);
            while (node != null)
            {
                var cmp = obj.CompareTo(node.valueT);
                if (cmp == 0)
                {
                    return node.valueT;
                }
                var delta = obj.DeltaTo(node.valueT);
                if (Math.Abs(closestDelta) >= Math.Abs(delta))
                {
                    if (Math.Abs(closestDelta) == Math.Abs(delta))
                    {
                        // 相等时取较小值
                        if (node.valueT.CompareTo(closest.valueT) < 0)
                        {
                            closest = node;
                            closestDelta = delta;
                        }
                    }
                    else
                    {
                        closest = node;
                        closestDelta = delta;
                    }
                }
                if (cmp < 0)
                {
                    node = node.left;
                }
                else
                {
                    node = node.right;
                }
            }
            return closest.valueT;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return _rawData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}