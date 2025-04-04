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
        private static AssetsBundleManager _assetsBundleManager;
        private static AddressableManager _addressableManager;
        
        public static AssetInitState initState =>
            _loadMode switch
            {
                LoadMode.AssetBundle => _assetsBundleManager.initState,
                LoadMode.Addressable => _addressableManager.initState,
                LoadMode.Resources => AssetInitState.Complete,
                _ => AssetInitState.Complete
            };

        public static float initProcess =>
            _loadMode switch
            {
                LoadMode.AssetBundle => _assetsBundleManager.initProcess,
                LoadMode.Addressable => _addressableManager.initProcess,
                LoadMode.Resources => 1f,
                _ => 0f
            };

        public static IAssetLoader SpawnLoader(string tag= "")
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    return _assetsBundleManager.SpawnLoader(tag);
                case LoadMode.Addressable:
                    return _addressableManager.SpawnLoader(tag);
                case LoadMode.Resources:
                    return new ResourceAssetLoader();
                default:
                    return _addressableManager.SpawnLoader(tag);
            }
        }
        
        public static void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    if(_assetsBundleManager != null) break;
                    _assetsBundleManager = new AssetsBundleManager(); 
                    _assetsBundleManager.Init(coroutineRunner, callBack);
                    break;
                case LoadMode.Addressable:
                    if(_addressableManager != null) break;
                    _addressableManager = new AddressableManager();
                    _addressableManager.Init(coroutineRunner, callBack);
                    break;
                case LoadMode.Resources:
                    break;
                default:
                    if(_assetsBundleManager != null) break;
                    _assetsBundleManager = new AssetsBundleManager();
                    _assetsBundleManager.Init(coroutineRunner, callBack);
                    break;
            }
        }

        public static void DeSpawnLoader(IAssetLoader assetLoader)
        {
            if(assetLoader == null) return;
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    _assetsBundleManager.DeSpawnLoader(assetLoader as AssetAssetLoader);
                    break;
                case LoadMode.Addressable:
                    _addressableManager.DeSpawnLoader(assetLoader as AddressableAssetLoader);
                    break;
                case LoadMode.Resources:
                    assetLoader.Deinit();
                    break;
                default:
                    _addressableManager.DeSpawnLoader(assetLoader as AddressableAssetLoader);
                    break;
            }
        }
        
        public static void DeSpawnAllLoader()
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    _assetsBundleManager.DeSpawnAllLoader();
                    break;
                case LoadMode.Addressable:
                    _addressableManager.DeSpawnAllLoader();
                    break;
                case LoadMode.Resources:
                    break;
                default:
                    _addressableManager.DeSpawnAllLoader();
                    break;
            }
        }
        
        public static void DeSpawnLoaderByTag(string tag)
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    _assetsBundleManager.DeSpawnLoaderByTag(tag);
                    break;
                case LoadMode.Addressable:
                    _addressableManager.DeSpawnLoaderByTag(tag);
                    break;
                case LoadMode.Resources:
                    break;
                default:
                    _addressableManager.DeSpawnLoaderByTag(tag);
                    break;
            }
        }

        public static void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    _assetsBundleManager.LoadScene(sceneName, onComplete, unLoadOtherScene);
                    break;
                case LoadMode.Addressable:
                    _addressableManager.LoadScene(sceneName, onComplete, unLoadOtherScene);
                    break;
                case LoadMode.Resources:
                    SceneManager.LoadScene(sceneName, unLoadOtherScene ? LoadSceneMode.Single : LoadSceneMode.Additive);
                    break;
                default:
                    _addressableManager.LoadScene(sceneName, onComplete, unLoadOtherScene);
                    break;
            }
        }

        public static void UnloadScene(string name)
        {
            switch (_loadMode)
            {
                case LoadMode.AssetBundle:
                    _assetsBundleManager.UnloadScene(name);
                    break;
                case LoadMode.Addressable:
                    _addressableManager.UnloadScene(name);
                    break;
                case LoadMode.Resources:
                    SceneManager.UnloadSceneAsync(name);
                    break;
                default:
                    _addressableManager.UnloadScene(name);
                    break;
            }
        }
    }
}