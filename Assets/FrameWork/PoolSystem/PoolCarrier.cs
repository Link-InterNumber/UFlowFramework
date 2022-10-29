using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LinkFrameWork.PoolSystem
{
    public class PoolCarrier : IPoolCarrier
    {
        private int _maxCount;
        private readonly List<int> _nodeIndexList = new();
        private List<IPoolable> _nodes;
        private IPoolable _prefab;
        private int _spawnIndex = 1;

        public int Count => _nodes?.Count ?? 0;

        public int MaxCount
        {
            get => _maxCount;
            set
            {
                _maxCount = value;
                while (Count > _maxCount)
                {
                    var node = _nodes.First();
                    _nodes.RemoveAt(0);
                    _nodeIndexList.Remove(node.SpawnIndex);
                    node.DestroyNode();
                }
            }
        }

        public void InitPool(IPoolable prefab, int maxCount = 20)
        {
            _prefab = prefab;
            _nodes = new List<IPoolable>();
            MaxCount = maxCount;
        }

        public IPoolable GetPrefab()
        {
            return _prefab;
        }

        public IPoolable GetNode()
        {
            if (!_nodes.Any())
            {
                var cloned = _prefab.Clone();
                cloned.SpawnIndex = _spawnIndex;
                _spawnIndex++;
                return cloned;
            }

            var node = _nodes.First();
            _nodes.RemoveAt(0);
            _nodeIndexList.Remove(node.SpawnIndex);
            return node;
        }

        public void PushNode(IPoolable node)
        {
            if (_nodeIndexList.Contains(node.SpawnIndex) || Count >= MaxCount)
            {
                node.DestroyNode();
                return;
            }

            _nodes.Add(node);
            _nodeIndexList.Add(node.SpawnIndex);
        }

        public void Clear()
        {
            foreach (var node in _nodes) node?.DestroyNode();

            _nodes.Clear();
            _nodeIndexList.Clear();
        }

        public void InitPool(GameObject prefab, int maxCount = 20)
        {
            _prefab = prefab.GetComponent<IPoolable>();
            _nodes = new List<IPoolable>();
            MaxCount = maxCount;
        }

        public void ClearWithoutDestroy()
        {
            _nodes.Clear();
            _nodeIndexList.Clear();
        }
    }
}