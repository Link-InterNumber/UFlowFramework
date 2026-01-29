using System;
using System.Text;
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
        
        public static AssetInitState initState => _assetManager?.initState ?? AssetInitState.Complete;

        public static float initProcess => _assetManager?.initProcess ?? 0f;

        public static IAssetLoader SpawnLoader(string tag= "")
        {
            return _assetManager?.SpawnLoader(tag) ?? new ResourceAssetLoader();
        }
        
        public static void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            _loaderYieldInstructionPool = new LoaderYieldInstructionPool();
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
        }

        public static void DeSpawnLoader(IAssetLoader assetLoader)
        {
            if(assetLoader == null) return;
            if(_assetManager != null) _assetManager.DeSpawnLoader(assetLoader);
            else assetLoader.Deinit();
        }
        
        public static void DeSpawnAllLoader()
        {
            _assetManager?.DeSpawnAllLoader();
        }

        public static void DeSpawnLoaderByTag(string tag)
        {
            _assetManager?.DeSpawnLoaderByTag(tag);
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

        public static void PreloadAsset(string path)
        {
            _assetManager?.PreloadAsset(path);
        }

        public static PrepareHandler Prepare(string[] labels, Action onComplete, bool isConcurrent = false)
        {
            if(_assetManager == null) return null;
            return _assetManager.Prepare(labels, onComplete, isConcurrent);
        }

        public static void Unprepare(PrepareHandler handler)
        {
            if(_assetManager == null || handler == null) return;
            _assetManager.Unprepare(handler); 
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

        public static LoaderYieldInstruction<T> GetLoadHandler<T>(string path) where T : class
        {
            return _loaderYieldInstructionPool.Get<T>(path);
        }
        
        public static void ReleaseLoadHandler<T>(ILoaderYieldInstruction item) where T : class
        {
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