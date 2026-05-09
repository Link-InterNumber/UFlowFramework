using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PowerCellStudio
{
    public class AssetUtils
    {
        public enum LoadMode
        {
            AssetBundle,
            Addressable,
            Resources,
        }
        
        private static LoadMode _loadMode = LoadMode.Addressable;
        public static LoadMode loadMode => _loadMode;
        private static IAssetManager _assetManager;
        
        private static LoaderYieldInstructionPool _loaderYieldInstructionPool;
        private static AssetLoaderPool _assetLoaderPool;
        private static PoolableObjectPool _preLoaderPool;
        
        public static AssetInitState initState => _assetManager?.initState ?? AssetInitState.Complete;

        public static float initProcess => _assetManager?.initProcess ?? 0f;
        
        public static void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            _loaderYieldInstructionPool = new LoaderYieldInstructionPool();
            _preLoaderPool = new PoolableObjectPool(() => new AssetBatchLoader(), 10, 100);
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    if (_assetManager != null) break;
                    _assetManager = new AssetsBundleManager();
                    _assetManager.Init(coroutineRunner, callBack);
                    break;
                case LoadMode.Addressable:
                    if (_assetManager != null) break;
                    _assetManager = new AddressableManager();
                    _assetManager.Init(coroutineRunner, callBack);
                    break;
                case LoadMode.Resources:
                    if (_assetManager != null) break;
                    _assetManager = new ResourceManager();
                    _assetManager.Init(coroutineRunner, callBack);
                    break;
                default:
                    if (_assetManager != null) break;
                    _assetManager = new AssetsBundleManager();
                    _assetManager.Init(coroutineRunner, callBack);
                    break;
            }
            _assetLoaderPool = new AssetLoaderPool(_assetManager.CreateLoader);
        }

        /// <summary>
        /// 创建一个资源加载器实例。
        /// <para>Create an asset loader instance.</para>
        /// </summary>
        /// <param name="tag">加载器标签。<para>Loader tag.</para></param>
        /// <returns>资源加载器实例。<para>Asset loader instance.</para></returns>
        public static IAssetLoader SpawnLoader(string tag= "")
        {
            return _assetLoaderPool?.Spawn(tag) ?? new ResourceAssetLoader();
        }

        /// <summary>
        /// 回收指定的资源加载器实例。
        /// <para>Despawn the specified asset loader instance.</para>
        /// </summary>
        /// <param name="loader">要回收的加载器。<para>Loader to despawn.</para></param>
        public static void DeSpawnLoader(IAssetLoader assetLoader)
        {
            _assetLoaderPool?.DeSpawn(assetLoader);
        }
        
        /// <summary>
        /// 回收所有资源加载器实例。
        /// <para>Despawn all asset loader instances.</para>
        /// </summary>
        public static void DeSpawnAllLoader()
        {
            _assetLoaderPool?.DeSpawnAllLoader();
        }

        /// <summary>
        /// 根据标签回收所有对应的资源加载器实例。
        /// <para>Despawn all asset loaders by tag.</para>
        /// </summary>
        /// <param name="tag">加载器标签。<para>Loader tag.</para></param>
        public static void DeSpawnLoaderByTag(string tag)
        {
            _assetLoaderPool?.DeSpawnLoaderByTag(tag);
        }

        public static void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            if(_assetManager != null) 
                _assetManager.LoadScene(sceneName, onComplete, unLoadOtherScene);
            else 
                SceneManager.LoadScene(sceneName, unLoadOtherScene ? LoadSceneMode.Single : LoadSceneMode.Additive);
        }

        public static void UnloadScene(string name)
        {
            if(_assetManager != null)
                _assetManager.UnloadScene(name);
            else 
                SceneManager.UnloadSceneAsync(name);
        }

        /// <summary>
        /// 批量下载资源。
        /// <para>Download assets in batch by labels.</para>
        /// </summary>
        /// <param name="labels">资源数组。<para>Asset labels array.</para></param>
        /// <param name="onComplete">下载完成回调。<para>Callback when download is complete.</para></param>
        /// <param name="isConcurrent">是否并发下载。<para>Whether to download concurrently.</para></param>
        /// <returns>下载处理句柄。<para>Download handler.</para></returns>
        public static AssetBatchLoader SpawnBatchLoader(IAssetLoader loader, string[] labels, Action onComplete, bool isConcurrent = false)
        {
            var handler = _preLoaderPool.Get() as AssetBatchLoader;
            handler.Prepare(loader, labels, onComplete, isConcurrent);
            return handler;
        }

        /// <summary>
        /// 回收批量下载处理句柄。
        /// <para>Despawn batch download handler.</para>
        /// </summary>
        /// <param name="handler">下载处理句柄。<para>Download handler.</para></param>
        public static void DespawnBatchLoader(AssetBatchLoader handler)
        {
            if (handler == null) return;
            _preLoaderPool.Release(handler);
        }

        public static string EditorCheckPath(string paths)
        {
            if (paths.Contains('\\'))
            {
                AssetLog.LogError("\\ exists in the asset path, replace Path.Combine() with AssetUtils.CombinePaths()");
                var result = paths.Replace('\\', '/');
                return result;
            }
            return paths;
        }

        public static string CombinePaths(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                return string.Empty;
            }

            // Creating a buffer on the stack, ensure it's large enough or dynamically handle large paths
            Span<char> pathBuffer = stackalloc char[256];
            int position = 0;

            for (int i = 0; i < paths.Length; i++)
            {
                string pathSegment = paths[i];
                if (string.IsNullOrEmpty(pathSegment)) continue;
                var segmentSpan = pathSegment.AsSpan();

                // Trim leading slashes
                int start = 0;
                while (start < segmentSpan.Length && (segmentSpan[start] == '/' || segmentSpan[start] == '\\'))
                {
                    start++;
                }

                // Trim trailing slashes
                int end = segmentSpan.Length - 1;
                while (end >= start && (segmentSpan[end] == '/' || segmentSpan[end] == '\\'))
                {
                    end--;
                }

                // Copy characters into the buffer, replacing '\\' with '/'
                for (int j = start; j <= end; j++)
                {
                    char c = segmentSpan[j] == '\\' ? '/' : segmentSpan[j];
                    pathBuffer[position++] = c;
                }

                // Add a separator unless it's the last segment
                if (i < paths.Length - 1)
                {
                    pathBuffer[position++] = '/';
                }
            }

            return new string(pathBuffer.Slice(0, position));
        }

        public static LoaderYieldInstruction<T> GetLoadHandler<T>(string path, bool autoRelease = false) where T : class
        {
            var handler = _loaderYieldInstructionPool.Get<T>(path);
            if (autoRelease)
            {
                handler.AddAutoReleaseHandle(h =>
                {
                    _loaderYieldInstructionPool.Release<T>(h);
                });
            }
            return handler;
        }
        
        public static void ReleaseLoadHandler<T>(ILoaderYieldInstruction item) where T : class
        {
            if (item == null) return;
            if (item.autoRelease)
            {
                AssetLog.LogError("Trying to release a handler that is already set to auto-release. This may indicate a logic error.");
                item.Dispose();
                return;
            }
            _loaderYieldInstructionPool.Release<T>(item);
        }

        public static void ClearUnusedAsset()
        {
            _assetManager?.ClearUnusedAsset();
            Resources.UnloadUnusedAssets();
        }

        public static bool TryGetSubAssetName(string path, out string mainPath, out string subAssetName)
        {
            mainPath = null;
            subAssetName = null;
            if (string.IsNullOrEmpty(path) || !path[path.Length - 1].Equals(']')) return false;
            var length = path.Length;
            var open = -1;
            for (int i = length - 1; i >= 0; i--)
            {
                if (path[i] != '[') continue;
                open = i;
                break;
            }
            if (open < 0) return false;
            mainPath = path.Substring(0, open);
            subAssetName = path.Substring(open + 1, path.Length - open - 1);
            return !string.IsNullOrEmpty(subAssetName);
        }
    }
}