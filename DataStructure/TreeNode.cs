using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class TreeNode<T> : IDisposable
    {
        private T _value;
        private TreeNode<T> _parent;
        private List<TreeNode<T>> _child;

        public Vector2 Position { get; set; }
        
        public T Value => _value;
        public TreeNode<T> Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public TreeNode<T> Root
        {
            get 
            {
                var visited = new HashSet<TreeNode<T>>();
                var root = this;
                while (root.Parent != null && visited.Add(root))
                {
                    root = root.Parent;
                }
                return root;
            }
        }

        public IReadOnlyList<TreeNode<T>> Child => _child;
        
        public TreeNode(T value)
        {
            _value = value;
            _child = new List<TreeNode<T>>();
        }

        public bool AddChild(T value)
        {
            var node = new TreeNode<T>(value);
            return AddChild(node);
        }

        public bool AddChild(TreeNode<T> node)
        {
            if (node == null || node == this || _child.Contains(node) || IsDescendantOf(node)) return false;
            if (node.Parent != null && node.Parent != this)
            {
                node.Parent.RemoveChild(node);
            }

            node.Parent = this;
            _child.Add(node);
            return true;
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            if (!_child.Contains(node)) return false;
            _child.Remove(node);
            if (node != null && node.Parent == this)
            {
                node.Parent = null;
            }
            return true;
        }

        public bool RemoveChild(T value)
        {
            var removed = false;
            for (var i = _child.Count - 1; i >= 0; i--)
            {
                var child = _child[i];
                if (!EqualityComparer<T>.Default.Equals(child.Value, value)) continue;

                _child.RemoveAt(i);
                if (child.Parent == this)
                {
                    child.Parent = null;
                }

                removed = true;
            }

            return removed;
        }
        
        public void AddAtFirst(T value)
        {
            var node = new TreeNode<T>(value);
            AddAtFirst(node);
        }

        public void AddAtFirst(TreeNode<T> node)
        {
            if (node == null) return;

            var root = Root;
            if (root == node) return;

            node.AddChild(root);
        }

        private bool IsDescendantOf(TreeNode<T> node)
        {
            var visited = HashSetPool<TreeNode<T>>.Get();
            var current = this;
            while (current != null && visited.Add(current))
            {
                if (current == node)
                {
                    HashSetPool<TreeNode<T>>.Release(visited);
                    return true;
                }
                current = current.Parent;
            }
            HashSetPool<TreeNode<T>>.Release(visited);
            return false;
        }

        public void Dispose()
        {
            var root = Root;
            _value = default(T);
            _parent = null;
            for (var i = 0; i < _child.Count; i++)
            {
                var node = _child[i];
                node.Dispose();
            }
            _child.Clear();
        }
    }
}