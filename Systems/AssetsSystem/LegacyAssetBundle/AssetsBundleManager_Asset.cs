using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private static Dictionary<string, LoaderYieldInstruction<Object>> _preloadHandles;
        private AssetBundleIndex _bundleIndex;
        // 计划加载的资源，key为BundleName，value为资源路径列表
        private LoadPlan _loadPlan;
        private LoadedCache<Object> _loadedAssets;
        private AssetLoadingHolder<Object> _loadingAssets;

        private IEnumerator InitPathMap()
        {
            initProcess = 0f;
            _bundleIndex = new AssetBundleIndex();
            yield return _bundleIndex.Init();
            initProcess = 1f;
        }

        public bool IsAssetLoading(string assetPath)
        {
            return _loadingAssets.IsLoading(assetPath);
        }

        public void PreloadAsset(string path)
        {
            if (_preloadHandles == null) _preloadHandles = new Dictionary<string, LoaderYieldInstruction<Object>>();
            if (_preloadHandles.ContainsKey(path)) return;
            var loadAssetRequest = AssetUtils.GetLoadHandler<Object>(path);
            LoadAssetAsync<Object>(path, loadAssetRequest);
            _preloadHandles.Add(path, loadAssetRequest);
        }

        public void DelAssetRef(string assetPath, int delCount = 1)
        {
            if (_loadedAssets.TryDelRef(assetPath, delCount, out var asset))
            {
                _loadedAssets.RemoveCache(assetPath);
                Resources.UnloadAsset(asset);
                var bundleName = _bundleIndex.GetBundleNameByAsset(assetPath);
                DelBundleRef(bundleName, 1);
            }
        }

        public T LoadAsset<T>(string assetPath)
            where T : Object
        {
            if (_loadedAssets.TryGetCache(assetPath, out var cache) && cache is T cachedAsset && cachedAsset)
            {
                _loadedAssets.AddRef(assetPath, 1);
                return cachedAsset;
            }

            if (_preloadHandles.TryGetValue(assetPath, out var handle) && handle.isDone)
            {
                _preloadHandles.Remove(assetPath);
                var preloadAsset = handle.asset as T;
                AssetUtils.ReleaseLoadHandler<T>(handle);
                return preloadAsset;
            }
            var bundleName = _bundleIndex.GetBundleNameByAsset(assetPath);
            if (string.IsNullOrEmpty(bundleName))
            {
                return null;
            }
            if (!GetAssetBundle(bundleName, out var bundle)) 
                return null;

            T asset = null;
            if (AssetUtils.TryGetSubAssetName(assetPath, out var mainPath, out var subAssetName))
            {
                var assets = bundle.LoadAssetWithSubAssets<T>(mainPath);
                if (assets == null)
                    return null;

                foreach (var a in assets)
                {
                    if (a == null || a.name != subAssetName || a is not T matched) 
                        continue;
                    asset = matched;
                    break;
                }
            }
            else
            {
                asset = bundle.LoadAsset<T>(assetPath);
            }

            if (asset)
            {
                _loadedAssets.AddCache(assetPath, asset);
                _loadedAssets.AddRef(assetPath, 1);
            }
            return asset;
        }

        public void LoadAssetAsync<T>(string assetPath, LoaderYieldInstruction<T> loadAssetRequest)
            where T : Object
        {
            if (loadAssetRequest == null) return;
            if (_loadedAssets.TryGetCache(assetPath, out var asset) && asset is T cachedAsset && cachedAsset)
            {
                _loadedAssets.AddRef(assetPath, 1);
                loadAssetRequest.SetAsset(cachedAsset);
                return;
            }

            if (_preloadHandles.ContainsKey(assetPath))
            {
                var handle = _preloadHandles[assetPath];
                _preloadHandles.Remove(assetPath);
                if (handle.isDone)
                {
                    loadAssetRequest.SetAsset(handle.asset as T);
                    AssetUtils.ReleaseLoadHandler<T>(handle);
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
            if (_loadingAssets.IsLoading(assetPath))
            {
                _loadingAssets.AddLoadingHandle(assetPath, loadAssetRequest as LoaderYieldInstruction<Object>);
                return;
            }
            var bundleName = _bundleIndex.GetBundleNameByAsset(assetPath);
            if (string.IsNullOrEmpty(bundleName))
            {
                loadAssetRequest.SetAsset(null);
                return;
            }
            _loadPlan.AddPlan(bundleName, assetPath, typeof(T));
            _loadingAssets.AddLoadingHandle(assetPath, loadAssetRequest as LoaderYieldInstruction<Object>);
            GetAssetsBundleAsync(bundleName);
        }

        private void GetAssetFromBundleAsync(AssetBundle bundle, string bundleName, string assetPath, Type assetType)
        {
            if (!bundle)
            {
                _loadingAssets.SetLoaded(assetPath, null);
                return;
            }

            if (AssetUtils.TryGetSubAssetName(assetPath, out var mainPath, out var subAssetName))
            {
                var assetRequest = bundle.LoadAssetWithSubAssetsAsync(mainPath, assetType);
                assetRequest.completed += operation =>
                {
                    var operationHandle = operation as AssetBundleRequest;
                    var assets = operationHandle?.allAssets;
                    if (assets == null)
                    {
                        _loadingAssets.SetLoaded(assetPath, null);
                        DelBundleRef(bundleName, 1);
                        return;
                    }
                    foreach (var a in assets)
                    {
                        if (a == null) continue;
                        if (a.name == subAssetName)
                        {
                            _loadedAssets.AddCache(assetPath, a);
                            var refCount = _loadingAssets.SetLoaded(assetPath, a);
                            _loadedAssets.AddRef(assetPath, refCount);
                            return;
                        }
                    }
                    _loadingAssets.SetLoaded(assetPath, null);
                    DelBundleRef(bundleName, 1);
                };
            }
            else
            {
                var assetRequest = bundle.LoadAssetAsync(assetPath, assetType);
                assetRequest.completed += (operation) =>
                {
                    var operationHandle = operation as AssetBundleRequest;
                    if(operationHandle == null)
                    {
                        _loadingAssets.SetLoaded(assetPath, null);
                        DelBundleRef(bundleName, 1);
                        return;
                    }
                    var asset = operationHandle.asset;
                    _loadedAssets.AddCache(assetPath, asset);
                    var refCount = _loadingAssets.SetLoaded(assetPath, asset);
                    _loadedAssets.AddRef(assetPath, refCount);
                };
            }
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