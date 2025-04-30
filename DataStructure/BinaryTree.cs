using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using UnityEngine;

namespace PowerCellStudio
{    
    public class BinaryTree<T> : IEnumerable<T> where T : IComparable<T> 
    {
        private class BinaryTreeNode<T> : IComparable<BinaryTreeNode<T>> 
        {
            public int deepLv;
            public T valueT;

            public BinaryTreeNode<T> left;

            public BinaryTreeNode<T> right;

            public BinaryTreeNode(T v, int deepLv)
            {
                valueT = v;
                lv = deepLv;
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
        }

        public void Insert(T obj)
        {
            if (obj == null || _objects.Contains(obj)) return;
            _objects.Add(obj);
            _rawData.Add(obj);
            _nodeCount ++;
        }

        public bool Remove(T obj)
        {
            if (obj == null || !_objects.Contains(obj)) return false;
            _objects.Remove(obj);
            int index = _rawData.BinarySearch(item);
            if (index > -1) 
            {
                _rawData.RemoveAt(index);
                _nodeCount ++;
            }
        }

        public void Build()
        {
            _rawData.Sort();
            var centerIndex = (int)Math.floor(_rawData.Length / 2f);
            _deep = 1;
            _root = new BinaryTreeNode<T>(_rawData[centerIndex], _deep);
            _root.left = BuildHandler(_root, 0 , centerIndex);
            _root.right = BuildHandler(_root, centerIndex + 1, _rawData.Length - centerIndex - 1);
        }

        private BinaryTreeNode<T> BuildHandler(BinaryTreeNode<T> root, int startIndex, int subListLength)
        {
            if (subListLength < 1) return null;
            var deep = root.deep + 1;
            if (subListLength == 1) return new BinaryTreeNode<T>(_rawData[startIndex], deep);
            var centerIndex = (int) Math.floor((startIndex + subListLength - 1) / 2f);
            var newNode = new BinaryTreeNode<T>(_rawData[centerIndex], deep);
            newNode.left = BuildHandler(newNode, startIndex , centerIndex - startIndex);
            newNode.right = BuildHandler(newNode, centerIndex + 1, subListLength + startIndex - 1 - centerIndex);
            return newNode;
        }

        private int GetIndex(T obj)
        {
            int index = _rawData.BinarySearch(item);
            return index > -1? index : -1;
        }
        
        public T Find(int objPos, bool approximately = true)
        {
            if (_isLeaf)
            {
                if (_objectNum == 0) return default;
                if (!approximately)
                {
                    foreach (var quadTreeNode in _objects)
                    {
                        if (quadTreeNode.ToInt().Equals(objPos)) return quadTreeNode;
                    }
                    return default;
                }
                return _objects.MinBy(o => Mathf.Abs(objPos - o.ToInt())).First();
            }
            var index = GetIndex(objPos);
            return _children[index].Find(objPos, approximately);
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