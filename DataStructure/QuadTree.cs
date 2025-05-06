using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IQuadTreeItem : IToVector2 {}
    
    public class QuadTree<T> where T : class, IQuadTreeItem
    {
        private class QuadTreeNode<KT>: IDisposable 
            where KT :  class, IQuadTreeItem
        {
            public KT[] children;
            public int level;
            public int count;
            public Vector2 center;
            public Vector2 extends;
            public bool isLeaf => nodes == null;
            public QuadTreeNode<KT>[] nodes;
            public QuadTreeNode<KT> parent;

            public int maxCount;
            public int maxLv;

            public QuadTreeNode(QuadTreeNode<KT> root, Vector2 centerPos)
            {
                parent = root;
                if (root != null)
                {
                    level = root.level + 1;
                    maxCount = root.children.Length;
                    extends = root.extends * 0.5f;
                    maxLv = root.maxLv;
                    children = new KT[maxCount];
                }
                count = 0;
                center = centerPos;
            }

            public bool InRange(Vector2 pos)
            {
                var min = center - extends;
                var max = center + extends;
                return pos.x < max.x && pos.x >= min.x &&
                    pos.y < max.y && pos.y >= min.y;
            }

            public KT FindNearest(Vector2 pos)
            {
                if (nodes != null)
                {
                    var index = GetIndex(pos);
                    return nodes[index].FindNearest(pos);
                }
                return children?.MinBy(o => Vector2.Distance(o.ToVector(), pos)).First() ?? null;
            }

            public KT Find(Vector2 pos)
            {
                if (!InRange(pos)) return null;
                if (nodes != null)
                {
                    var index = GetIndex(pos);
                    return nodes[index].Find(pos);
                }
                for (var i = 0; i < count; i++)
                {
                    var child = children[i];
                    if (child.ToVector().Equals(pos))
                    {
                        return child;
                    }
                }
                return null;
            }

            public int GetIndex(Vector2 objV2)
            {
                var delta = -center + objV2;
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

            public void Add(KT v, Vector2 pos)
            {
                if (nodes != null)
                {
                    var index = GetIndex(pos);
                    nodes[index].Add(v, pos);
                    return;
                }
                if(count + 1 > children.Length && level < maxLv)
                {
                    Split();
                    Add(v, pos);
                    return;
                }
                if (count >= children.Length) Array.Resize(ref children, children.Length + maxCount);
                children[count] = v;
                count ++;
            }

            public bool Remove(KT v, Vector2 pos)
            {
                if (nodes != null)
                {
                    var index = GetIndex(pos);
                    return nodes[index].Remove(v, pos);
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
                if (removeIndex < 0) return false;
                children[removeIndex] = null;
                for (var i = removeIndex + 1; i < count; i++)
                {
                    children[i - 1] = children[i];
                }
                children[count - 1] = null;
                count--;
                if (count == 0)
                {
                    parent?.TryMerge();
                }
                return true;
            }

            private void Split()
            {
                nodes = new QuadTreeNode<KT>[4];
                for (var i = 0; i < 4; i++)
                {
                    var newCenter = i switch
                    {
                        0 => center + new Vector2(extends.x * -0.5f, extends.y * -0.5f),
                        1 => center + new Vector2(extends.x * -0.5f, extends.y * 0.5f),
                        2 => center + new Vector2(extends.x * 0.5f, extends.y * -0.5f),
                        3 => center + new Vector2(extends.x * 0.5f, extends.y * 0.5f),
                        _ => center + new Vector2(extends.x * -0.5f, extends.y * -0.5f),
                    } ;
                    nodes[i] = new QuadTreeNode<KT>(this, newCenter);
                }

                for (var i = 0; i < count; i++)
                {
                    var child = children[i];
                    Add(child, child.ToVector());
                }
                Array.Clear(children, 0, children.Length);
                children = null;
                count = 0;
            }

            public void TryMerge()
            {
                if (nodes == null) return;
                var totalCount = 0;
                for (var i = 0; i < 4; i++)
                {
                    if(!nodes[i].isLeaf) return;
                    totalCount += nodes[i].count;
                }
                if (totalCount > maxCount) return;
                children = new KT[maxCount];
                count = 0;
                for (var i = 0; i < 4; i++)
                {
                    Array.Copy(children, count, nodes[i].children, 0, nodes[i].count);
                    count += nodes[i].count;
                    nodes[i].Dispose();
                }
                nodes = null;
            }

            public void Dispose()
            {
                children = null;
                parent = null;
                count = 0;
                level = 0;
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        node.Dispose();
                    }
                }
                nodes = null;
            }
        }

        private HashSet<T> _objects;
        private QuadTreeNode<T> _root;

        public int maxLevel => _root.maxLv;
        public Vector2 center => _root.center;
        public Vector2 extends => _root.extends;

        public int Count => _objects.Count;

        public QuadTree(Vector2 center, Vector2 extends, int maxCount = 10, int maxLv = 5)
        {
            _root = CreateRoot(center, extends, maxCount, maxLv);
            _objects = new HashSet<T>();
        }

        private static QuadTreeNode<T> CreateRoot(Vector2 center, Vector2 extends, int maxCount, int maxLv)
        {
            var root = new QuadTreeNode<T>(null, center);
            root.level = 1;
            root.extends = extends;
            root.maxCount = maxCount;
            root.maxLv = maxLv;
            root.children = new T[maxCount];
            return root;
        } 

        public void Clear()
        {
            var newRoot = CreateRoot(_root.center, _root.extends, _root.maxCount, _root.maxLv);
            _root.Dispose();
            _root = newRoot;
            _objects.Clear();
        }

        public void ReBuild()
        {
            var newRoot = CreateRoot(_root.center, _root.extends, _root.maxCount, _root.maxLv);
            _root.Dispose();
            _root = newRoot;

            foreach (var item in _objects)
            {
                var pos = item.ToVector();
                _root.Add(item, pos);
            }
        }

        public void Insert(T obj)
        {
            if(_objects.Contains(obj))
                return;
            _objects.Add(obj);
            var pos = obj.ToVector();
            _root.Add(obj, pos);
        }

        public bool Remove(T obj)
        {
            if(!_objects.Remove(obj))
                return false;
            var pos = obj.ToVector();
            return _root.Remove(obj, pos);
        }

        public T[] GetLeaf(Vector2 pos, out int count)
        {
            var branch = _root;
            if(!branch.isLeaf)
            {
                var index = branch.GetIndex(pos);
                branch = branch.nodes[index];
            }
            count = branch.count;
            return branch.children;
        }

        public T[] GetBlock(Vector2 pos)
        {
            var root = _root;
            if(!root.isLeaf)
            {
                var index = root.GetIndex(pos);
                root = root.nodes[index];
            }
            root = root.parent != null ? root.parent : root;
            var count = root.nodes.Sum(o => o.count);
            var result = new T[count];
            var temp = 0;
            for (var i = 0; i < root.nodes.Length; i++)
            {
                var branch = root.nodes[i];
                for (var j = 0; j < branch.count; j++)
                {
                    result[temp] = branch.children[j];
                    temp++;
                }
            }
            return result;
        }
        
        public IEnumerator<T> GetLeafEnumerator(Vector2 pos)
        {
            var branch = _root;
            if(!branch.isLeaf)
            {
                var index = branch.GetIndex(pos);
                branch = branch.nodes[index];
            }
            var count = branch.count;
            for (var i = 0; i < count; i ++)
            {
                yield return branch.children[i];
            }
        }

        public IEnumerator<T> GetBlockEnumerator(Vector2 pos)
        {
            var root = _root;
            if(!root.isLeaf)
            {
                var index = root.GetIndex(pos);
                root = root.nodes[index];
            }
            root = root.parent != null ? root.parent : root;
            for (var i = 0; i < root.nodes.Length; i++)
            {
                var branch = root.nodes[i];
                for (var j = 0; j < branch.count; j++)
                {
                    yield return branch.children[j];
                }
            }
        }
        
        public T Find(Vector2 pos, bool approximately = true)
        {
            if (approximately)
            {
                return _root.FindNearest(pos);
            }
            return _root.Find(pos);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _objects.GetEnumerator();
        }

        // IEnumerator IEnumerable.GetEnumerator()
        // {
        //     return GetEnumerator();
        // }
    }
}