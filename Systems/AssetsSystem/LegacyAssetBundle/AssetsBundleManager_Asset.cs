using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private static Dictionary<string, LoaderYieldInstruction<Object>> _preloadHandles;

        public void PreloadAsset(string path)
        {
            if (_preloadHandles == null) _preloadHandles = new Dictionary<string, LoaderYieldInstruction<Object>>();
            if (_preloadHandles.ContainsKey(path)) return;
            var loadAssetRequest = new LoaderYieldInstruction<Object>(path);
            var bundleName = GetBundleNameByAsset(path);
            LoadAssetAsync<Object>(bundleName, path, loadAssetRequest);
            _preloadHandles.Add(path, loadAssetRequest);
        }

        public LoaderYieldInstruction<T> LoadAsset<T>(string bundleName, string assetPath)
            where T : Object
        {
            if (_preloadHandles.ContainsKey(assetPath))
            {
                var handle = _preloadHandles[assetPath];
                _preloadHandles.Remove(assetPath);
                return handle as LoaderYieldInstruction<T>;
            }

            var loadAssetRequest = new LoaderYieldInstruction<T>(assetPath);
            if (GetAssetBundle(bundleName, out var bundle))
            {
                if (bundle == null)
                {
                    loadAssetRequest.SetAsset(null);
                    return loadAssetRequest;
                }
                var asset = bundle.LoadAsset<T>(assetPath);
                loadAssetRequest.SetAsset(asset);
            }
            else
            {
                loadAssetRequest.SetAsset(null);
            }
            return loadAssetRequest;
        }
        
        public void LoadAssetAsync<T>(string bundleName, string assetPath, LoaderYieldInstruction<T> loadAssetRequest)
            where T : Object
        {
            if (loadAssetRequest == null) return;
            if (_preloadHandles.ContainsKey(assetPath))
            {
                var handle = _preloadHandles[assetPath];
                _preloadHandles.Remove(assetPath);
                if (handle.isDone)
                {
                    loadAssetRequest.SetAsset(handle.asset as T);
                }
                else
                {
                    handle.OnLoadCompleted((a, path) =>
                    {
                        loadAssetRequest.SetAsset(a as T);
                    });
                }
                return;
            }

            var loadBundleRequest = GetAssetsBundleAsync(bundleName);
            if (loadBundleRequest.isDone)
            {
                var bundle = _loadedBundleDic[bundleName].Bundle;
                GetAssetFromBundleAsync(bundle, assetPath, loadAssetRequest);
                return;
            }
            loadBundleRequest.OnLoadCompleted((bundle, bundleName) =>
            {
                GetAssetFromBundleAsync(bundle, assetPath, loadAssetRequest);
            });
        }

        private void GetAssetFromBundleAsync<T>(AssetBundle bundle, string assetPath, LoaderYieldInstruction<T> loadAssetRequest)
            where T : Object
        {
            if (!bundle)
            {
                loadAssetRequest.SetAsset(null);
                return;
            }
            var assetRequest = bundle.LoadAssetAsync<T>(assetPath);
            assetRequest.completed += (operation) =>
            {
                var operationHandle = operation as AssetBundleRequest;
                if(operationHandle == null)
                {
                    loadAssetRequest.SetAsset(null);
                    return;
                }
                var asset = operationHandle.asset as T;
                loadAssetRequest.SetAsset(asset);
            };
        }

        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            var handler = SceneManager.LoadSceneAsync(sceneName, unLoadOtherScene ? LoadSceneMode.Single : LoadSceneMode.Additive);
            handler.completed += (operation) =>
            {
                onComplete?.Invoke();
            };
        }

        public void UnloadScene(string name)
        {
            SceneManager.UnloadSceneAsync(name);
        }
    }
}