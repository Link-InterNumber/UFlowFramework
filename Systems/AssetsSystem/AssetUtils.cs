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
        // private static AssetsBundleManager _assetsBundleManager;
        // private static AddressableManager _addressableManager;
        
        public static AssetInitState initState => _assetManager?.initState ?? AssetInitState.Complete;

        public static float initProcess =>_assetManager?.initProcess??0f;

        public static IAssetLoader SpawnLoader(string tag= "")
        {
            return _assetManager?.SpawnLoader(tag) ?? new ResourceAssetLoader();
        }
        
        public static void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    if(_assetManager != null) break;
                    _assetManager = new AssetsBundleManager(); 
                    _assetManager.Init(coroutineRunner, callBack);
                    break;
                case LoadMode.Addressable:
                    if(_assetManager != null) break;
                    _assetManager = new AddressableManager();
                    _assetManager.Init(coroutineRunner, callBack);
                    break;
                case LoadMode.Resources:
                    break;
                default:
                    if(_assetManager != null) break;
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
    }
}