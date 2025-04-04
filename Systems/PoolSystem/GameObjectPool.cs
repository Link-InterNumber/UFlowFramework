using System.Collections;
using System.IO;
using UnityEngine;

namespace PowerCellStudio
{
    public class GameObjectPool : LinkPool<GameObject>
    {
        protected IAssetLoader assetLoader;
        protected GameObject _prefab;
        protected AssetLoadStatus _loadStatus;
        protected readonly string _tag;
        protected Transform _root;
        public bool autoDestroy = true;
        public AssetLoadStatus loadStatus => _loadStatus;
        public string tag => _tag;

        public GameObjectPool(string path, int maxSize, int initSize, Transform root)
        {
            _loadStatus = AssetLoadStatus.Loading;
            _maxSize = maxSize;
            assetLoader = AssetUtils.SpawnLoader(nameof(PoolManager));
            _root = root;
            _tag = $"{Path.GetFileNameWithoutExtension(path)}_{IndexGetter.instance.Get<GameObjectPool>()}";
            ApplicationManager.instance.StartCoroutine(InitPoolHandle(path, initSize));
        }

        public IEnumerator WaitForInitAsYieldInstruction()
        {
            while (_loadStatus == AssetLoadStatus.Loading)
            {
                yield return null;
            }
        }

        private IEnumerator InitPoolHandle(string path, int initSize)
        {
            _loadStatus = AssetLoadStatus.Loading;
            var handle = assetLoader.LoadAsYieldInstruction<GameObject>(path);
            yield return handle;
            _prefab = handle.asset;
            if (_prefab == null)
            {
                _loadStatus = AssetLoadStatus.Unload;
                AssetUtils.DeSpawnLoader(assetLoader);
                LinkLog.LogError($"Pool init failed, path: {path}");
                yield break;
            }

            _createFun = InstantiateFromPrefab;
            for (int i = 0; i < initSize; i++)
            {
                var go = InstantiateFromPrefab();
                go.SetActive(false);
                go.transform.SetParent(_root);
                _stack.Push(go);
            }

            _loadStatus = AssetLoadStatus.Loaded;
        }

        private int index = 0;
        private GameObject InstantiateFromPrefab()
        {
            var go = GameObject.Instantiate(_prefab);
            go.name = $"{_tag}^{index}";
            index++;
            if (index == int.MaxValue) index = 0;
            return go;
        }

        public override GameObject Get()
        {
            if(_loadStatus != AssetLoadStatus.Loaded) return null;
            var go = base.Get();
            go.SetActive(true);
            return go;
        }

        public override bool Release(GameObject obj)
        {
            if (!obj || !CanPool(obj)) return false;
            if (_set.Contains(obj)) return true;
            if (count == _maxSize)
            {
                if (autoDestroy) GameObject.Destroy(obj);
                return true;
            }
            _stack.Push(obj);
            _set.Add(obj);
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_root);
            return true;
        }
        
        public void ReleaseWithoutCheck(GameObject obj)
        {
            if (_set.Contains(obj)) return;
            if (count >= _maxSize)
            {
                if (autoDestroy) GameObject.Destroy(obj);
                return;
            }
            _stack.Push(obj);
            _set.Add(obj);
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_root);
        }

        public override void Clear()
        {
            foreach (var gameObject in _stack)
            {
                GameObject.Destroy(gameObject);
            }
            base.Clear();
        }

        public override void Dispose()
        {
            base.Dispose();
            AssetUtils.DeSpawnLoader(assetLoader);
            assetLoader = null;
            _prefab = null;
            _loadStatus = AssetLoadStatus.Unload;
        }

        public override bool CanPool(GameObject item)
        {
            return item.name.StartsWith(_tag);
        }
    }
}