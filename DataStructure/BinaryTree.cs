using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IBinaryTreeNode
    {
        public int ToInt();
    }
    
    public class BinaryTree<T> : IEnumerable<T> where T : class, IBinaryTreeNode 
    {
        private class BinaryTreeNode<T>
        {
            public T valueT;
            public BinaryTreeNode<T>[] children;

            public bool isLeaf => valueT != null;

            public BinaryTreeNode<T> left => children[0];

            public BinaryTreeNode<T> right => children[1];

            public BinaryTreeNode()
            {
                children = new BinaryTreeNode<T>[2];
            }
        }

        private int _maxObject;
        private int _maxLevel;
        private int _level;
        private HashSet<T> _objects;
        private int _objectNum;
        private BinaryTree<T>[] _children;
        private int _center;
        private int _extendsMin;
        private int _extendsMax;
        private bool _isLeaf;

        public int NodeCount => _objectNum;

        public int Count
        {
            get
            {
                return _isLeaf ? _objectNum : _children.Sum(o => o.Count);
            }
        }

        public BinaryTree(int maxLevel, int center, int extends, int maxCount = 10)
        {
            _maxLevel = maxLevel;
            _level = 0;
            _maxObject = maxCount;
            _objects = new HashSet<T>();
            _objectNum = 0;
            _children = new BinaryTree<T>[2];
            _center = center;
            _extendsMin = center - extends;
            _extendsMax = center + extends;
            _isLeaf = true;
            Split();
        }
        
        private BinaryTree(int maxLevel, int lv, int center, int extends, int maxCount)
        {
            _maxLevel = maxLevel;
            _level = lv;
            _maxObject = maxCount;
            _objects = new HashSet<T>();
            _objectNum = 0;
            _children = new BinaryTree<T>[2];
            _center = center;
            _extendsMin = center - extends;
            _extendsMax = center + extends;
            _isLeaf = true;
        }

        public void Clear()
        {
            _objects.Clear();
            foreach (var quadTree in _children)
            {
                quadTree.Clear();
            }
            Array.Clear(_children, 0, _children.Length);
            _isLeaf = true;
            _objectNum = 0;
        }

        private void Split()
        {
            _isLeaf = false;
            for (int i = 0; i < 2; i++)
            {
                var newCenter = _center;
                switch (i)
                {
                    case 0:
                        newCenter = Mathf.CeilToInt((_extendsMin + _center) * 0.5f);
                        break;
                    case 1:
                        newCenter = Mathf.FloorToInt((_extendsMax + _center) * 0.5f);
                        break;
                }
                _children[i] = new BinaryTree<T>(_maxLevel, _level + 1, newCenter, _center - _extendsMin, _maxObject);
            }

            foreach (var o in _objects)
            {
                Insert(o);
            }
            _objects.Clear();
            _objectNum = 0;
        }

        private void Combine()
        {
            if(_isLeaf) return;
            foreach (var quadTree in _children)
            {
                foreach (var quadTreeNode in quadTree)
                {
                    _objects.Add(quadTreeNode);
                    _objectNum++;
                }
            }
            Array.Clear(_children, 0, _children.Length);
            _isLeaf = true;
        }

        public void Insert(T obj)
        {
            if (_isLeaf)
            {
                if (_objectNum >= _maxObject && _level < _maxLevel)
                {
                    Split();
                    Insert(obj);
                    return;
                }
                _objects.Add(obj);
                _objectNum++;
            }
            else
            {
                var index = GetIndex(obj);
                _children[index].Insert(obj);
            }
        }

        public void Remove(T obj)
        {
            if (_isLeaf)
            {
                if (_objects.Remove(obj))
                {
                    _objectNum--;
                    _objectNum = Mathf.Max(0, _objectNum);
                }
            }
            else
            {
                var index = GetIndex(obj);
                _children[index].Remove(obj);
                if(_level == 0) return;
                if(!_children[0]._isLeaf) return;
                if (Count <= _maxObject)
                {
                    Combine();
                }
            }
        }

        private int GetIndex(T obj)
        {
            var objV2 = obj.ToInt();
            return GetIndex(objV2);
        }

        private int GetIndex(int objV2)
        {
            var delta = objV2 -_center;
            var index = 0;
            if (delta > 0)
            {
                index = 1;
            }
            return index;
        }

        public T[] GetBranch(T obj)
        {
            if (_isLeaf)
            {
                return _objects.ToArray();
            }
            var index = GetIndex(obj);
            return _children[index].GetBranch(obj);
        }
        
        public IEnumerable<T> GetBranch(int objPos)
        {
            if (_isLeaf)
            {
                return _objects;
            }
            var index = GetIndex(objPos);
            return _children[index].GetBranch(objPos);
        }

        public T Find(T obj, bool approximately = true)
        {
            if (_isLeaf)
            {
                if (_objectNum == 0) return default;
                if (!approximately && _objects.Contains(obj)) return obj;
                return _objects.MinBy(o => Mathf.Abs(obj.ToInt() - o.ToInt())).First();
            }
            var index = GetIndex(obj);
            return _children[index].Find(obj, approximately);
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
            if (_isLeaf)
            {
                foreach (var quadTreeNode in _objects)
                {
                    yield return quadTreeNode;
                }
            }
            foreach (var quadTreeNodes in _children)
            {
                using var enumerator = quadTreeNodes.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}