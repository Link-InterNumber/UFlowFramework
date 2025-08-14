using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace PowerCellStudio
{
    /// <summary>
    /// 使用Unity Resources接口实现的资源管理器。
    /// Resource manager implemented with Unity Resources API.
    /// </summary>
    public class ResourceManager : MonoBehaviour, IAssetManager
    {
        public AssetInitState initState { get; private set; } = AssetInitState.Complete;
        public float initProcess { get; private set; } = 0f;

        private MonoBehaviour coroutineRunner;

        private ObjectPool<ResourceAssetLoader> _pool;
        private Dictionary<long, ResourceAssetLoader> _activeLoader;

        public void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            _pool = new ObjectPool<ResourceAssetLoader>(() => new ResourceAssetLoader(),
                loader => loader.Init(),
                loader => loader.Deinit(),
                loader => loader.Deinit(), true, 10, 30);
            _activeLoader = new Dictionary<long, ResourceAssetLoader>();

            initState = AssetInitState.InitModule;
            initProcess = 0.5f;
            // Resources不需要复杂初始化，直接完成
            initState = AssetInitState.Complete;
            initProcess = 1f;
            callBack?.Invoke();
        }

        public IAssetLoader SpawnLoader(string tag)
        {
            var loader = _pool.Get();
            loader.tag = tag;
            _activeLoader.Add(loader.index, loader);
            return loader;
        }

        public void DeSpawnLoader(IAssetLoader assetLoader)
        {
            if (assetLoader == null) return;
            var resourceAssetLoader = assetLoader as ResourceAssetLoader;
            if (resourceAssetLoader == null) return;
            _activeLoader.Remove(resourceAssetLoader.index);
            if (!resourceAssetLoader.spawned)
            {
                resourceAssetLoader.Deinit();
                return;
            }
            _pool.Release(resourceAssetLoader);
        }

        public void DeSpawnAllLoader()
        {
            while (_activeLoader.Count > 0)
            {
                var loader = _activeLoader.First().Value;
                _activeLoader.Remove(loader.index);
                if(!loader.spawned)
                {
                    loader.Deinit();
                    continue;
                }
                _pool.Release(loader);
            }
        }

        public void DeSpawnLoaderByTag(string tag)
        {
            var loaders = _activeLoader.Where(o => o.Value.tag.Equals(tag)).ToArray();
            if(loaders.Length == 0) return;
            foreach (var resourceAssetLoader in loaders)
            {
                DeSpawnLoader(resourceAssetLoader.Value);
            }
        }

        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            coroutineRunner.StartCoroutine(LoadSceneCoroutine(sceneName, onComplete, unLoadOtherScene));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, Action onComplete, bool unLoadOtherScene)
        {
            if (unLoadOtherScene)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name != sceneName)
                    {
                        yield return SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return async;
            onComplete?.Invoke();
        }

        public void UnloadScene(string name)
        {
            SceneManager.UnloadSceneAsync(name);
        }

        public void PreloadAsset(string path)
        {
            // 预加载资源到内存
            Resources.Load(path);
        }

        public PrepareHandler Prepare(string[] labels, Action onComplete, bool isConcurrent = false)
        {
            if (labels == null || labels.Length == 0)
            {
                onComplete?.Invoke();
                return null;
            }
            // Resources不支持标签，这里简单实现为批量预加载
            var handler = new PrepareHandler();
            handler.OnComplete(onComplete);
            coroutineRunner.StartCoroutine(PrepareCoroutine(labels, handler, isConcurrent));
            return handler;
        }

        private IEnumerator PrepareCoroutine(string[] labels, PrepareHandler handler, bool isConcurrent)
        {
            var waitList = new ResourceRequest[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var path = labels[i];
                if (isConcurrent)
                {
                    var request = Resources.LoadAsync(path);
                    waitList[i] = request;
                }
                else
                {
                    handler.SetProcessValue(i * 1f / labels.Length);
                    var request = Resources.LoadAsync(path);
                    yield return request;
                    handler.Append(request.asset);
                }
            }
            if (isConcurrent)
            {
                var doneCount = 0;
                while (doneCount < labels.Length)
                {
                    doneCount = waitList.Count(o=>o.isDone);
                    handler.SetProcessValue(doneCount * 1f / labels.Length);
                    yield return null;
                }
                foreach (var request in waitList)
                {
                    handler.Append(request.asset);
                }
            }
            handler.SetProcessValue(1f);
            handler.SetComplete();
        }

        public void Unprepare(PrepareHandler handler)
        {
            if (handler == null) return;
            if (!handler.isDone)
            {
                ApplicationManager.instance.StartCoroutine(WaitForPrepareDone(handler));
                return;
            }
            foreach(var asset in handler.successLable)
            {
                if (asset == null) continue;
                Resources.UnloadAsset(asset as  UnityEngine.Object);
            }
            handler.Dispose();
        }

        private IEnumerator WaitForPrepareDone(PrepareHandler handler)
        {
            yield return handler;
            Unprepare(handler);
        }

        public void ClearUnusedAsset()
        {
            
        }
    }
}