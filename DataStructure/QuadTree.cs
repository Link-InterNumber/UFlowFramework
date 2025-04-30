using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IQuadTreeItem
    {
        public Vector2 ToVector();
    }
    
    public class QuadTree<T> : IEnumerable<T> where T : class, IQuadTreeItem 
    {
        private class QuadTreeNode<T>
        {
            public T[] children;
            public int level
            public int count;
            public Vector2 center;
            public Vector2 extends;
            public bool isLeaf => nodes != null;
            public QuadTreeNode<T>[][] nodes;
            public QuadTreeNode<T> parent;

            public QuadTreeNode<T>(QuadTreeNode<T> root, Vector2 centerPos)
            {
                parent = root;
                level = root.level + 1;
                children = new T[root.children.Length];
                count = 0;
                center = centerPos;
                extends = root.extends * 0.5f;
            }

            public void Add(T v)
            {
                if (nodes != null)
                {
                    var vec = v.ToVector();
                    var indexX = vec.x < center.x ? 0 : 1;
                    var indexY = vec.y < center.y ? 0 : 1;
                    nodes[indexX][indexY].Add(v);
                    return;
                }
                if(count + 1 > children.Length)
                {
                    Split();
                    Add(v);
                    return;
                }
                children[count] = v;
                count ++;
            }

            public void Remove(T v)
            {
                if (nodes != null)
                {
                    var vec = v.ToVector();
                    var indexX = vec.x < center.x ? 0 : 1;
                    var indexY = vec.y < center.y ? 0 : 1;
                    nodes[indexX][indexY].Remove(v);
                    return;
                }
                var removeIndex = -1;
                for (var i = 0; i < count; i++)
                {
                    var child = children[i];
                    if (child == v)
                    {
                        removeIndex = i;
                    }
                }
                if (removeIndex < 0) return;
                children[removeIndex] = null;
                Array.Copy(children, removeIndex + 1, children, removeIndex, count - removeIndex);
                children[count - 1] = null;
                count--;
                if(count == 0)
                {

                }
            }

            private void Split()
            {
                nodes = new QuadTreeNode<T>[][2];
                nodes[0] = new QuadTreeNode<T>[2];
                nodes[1] = new QuadTreeNode<T>[2];
                for (var i = 0; i < 2; i++)
                {
                    for (var j = 0; j < 2; j++)
                    {
                        nodes[i][j] = new QuadTreeNode<T>();
                    }
                }

                for (var i = 0; i < count; i++)
                {
                    var child = children[i];
                    Add(child);
                }
                Array.Clear(children 0, children.Length);
                children = null;
                count = 0;
            }
        }

        private int _maxObject;
        private int _maxLevel;
        private int _level;
        private HashSet<T> _objects = new HashSet<T>();
        private int _objectNum;
        private QuadTree<T>[] _children;
        private Vector2 _center;
        private Vector2 _extends;
        private bool _isLeaf;

        public int NodeCount => _objectNum;

        public int Count
        {
            get
            {
                return _isLeaf ? _objectNum : _children.Sum(o => o?.Count??0);
            }
        }

        public QuadTree(int maxLevel, Vector2 center, Vector2 extends, int maxCount = 10)
        {
            _maxLevel = maxLevel;
            _level = 0;
            _maxObject = maxCount;
            _objects = new HashSet<T>();
            _objectNum = 0;
            _children = new QuadTree<T>[4];
            _center = center;
            _extends = extends;
            _isLeaf = true;
            Split();
        }
        
        private QuadTree(int maxLevel, int lv, Vector2 center, Vector2 extends, int maxCount)
        {
            _maxLevel = maxLevel;
            _level = lv;
            _maxObject = maxCount;
            _objects = new HashSet<T>();
            _objectNum = 0;
            _children = new QuadTree<T>[4];
            _center = center;
            _extends = extends;
            _isLeaf = true;
        }

        public void Clear()
        {
            _objects?.Clear();
            foreach (var quadTree in _children)
            {
                quadTree?.Clear();
            }
            Array.Clear(_children, 0, _children.Length);
            _isLeaf = true;
            _objectNum = 0;
        }

        private void Split()
        {
            _isLeaf = false;
            for (int i = 0; i < 4; i++)
            {
                var newCenter = _center;
                switch (i)
                {
                    case 0:
                        newCenter += _extends * -0.5f;
                        break;
                    case 1:
                        newCenter.x = newCenter.x - _extends.x * 0.5f;
                        newCenter.y = newCenter.y + _extends.y * 0.5f;
                        break;
                    case 2:
                        newCenter.x = newCenter.x + _extends.x * 0.5f;
                        newCenter.y = newCenter.y - _extends.y * 0.5f;
                        break;
                    case 3:
                        newCenter += _extends * 0.5f;
                        break;
                }
                _children[i] = new QuadTree<T>(_maxLevel, _level + 1, newCenter, _extends * 0.25f, _maxObject);
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
            var objV2 = obj.ToVector();
            return GetIndex(objV2);
        }

        private int GetIndex(Vector2 objV2)
        {
            var delta = -_center + objV2;
            var index = 0;
            if (delta.x < 0 && delta.y > 0)
            {
                index = 1;
            }
            else if (delta.x > 0 && delta.y < 0)
            {
                index = 2;
            }
            else if (delta.x >= 0 && delta.y >= 0)
            {
                index = 3;
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
        
        public IEnumerable<T> GetBranch(Vector2 objPos)
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
                return _objects.MinBy(o => Vector2.Distance(obj.ToVector(), o.ToVector())).First();
            }
            var index = GetIndex(obj);
            return _children[index].Find(obj, approximately);
        }
        
        public T Find(Vector2 objPos, bool approximately = true)
        {
            if (_isLeaf)
            {
                if (_objectNum == 0) return default;
                if (!approximately)
                {
                    foreach (var quadTreeNode in _objects)
                    {
                        if (quadTreeNode.ToVector().Equals(objPos)) return quadTreeNode;
                    }
                    return default;
                }
                return _objects.MinBy(o => Vector2.Distance(objPos, o.ToVector())).First();
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
                if(quadTreeNodes == null) continue;
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