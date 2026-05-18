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

        public void DelAssetRef(string assetPath, int delCount = 1)
        {
            if (_loadingAssets.IsLoading(assetPath))
            {
                _loadingAssets.TryGetLoadingHandle(assetPath, out var handlerChain);
                if (handlerChain != null)
                {
                    var lastHandler = handlerChain[handlerChain.Count - 1];
                    handlerChain.RemoveAt(handlerChain.Count - 1);
                    lastHandler.SetAsset(null);
                    if (handlerChain.Count == 0)
                    {
                        _loadingAssets.RemoveLoading(assetPath);
                        var bundleName = _bundleIndex.GetBundleNameByAsset(assetPath);
                        _loadPlan.RemovePlan(bundleName, assetPath);
                    }
                }
                return;
            }
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

        public void LoadAssetsFromBundleAsync<T>(string bundleName, OnLoadSuccess<IList<T>> onSuccess, OnLoadFailed onFail) 
            where T : Object
        {
            var loadedBundle = GetUseableBundle(bundleName);
            if (loadedBundle)
            {
                loadedBundle.LoadAllAssetsAsync<T>().completed += operation =>
                {
                    var assetRequest = operation as AssetBundleRequest;
                    var assets = assetRequest?.allAssets;
                    if (assets == null)
                    {
                        onFail?.Invoke();
                        return;
                    }
                    onSuccess?.Invoke(assets as IList<T>);
                };
            }
            Action<AssetBundle> onLoaded = bundle =>
            {
                if (bundle == null)
                {
                    onFail?.Invoke();
                    return;
                }
                AddBundleRef(bundleName, 1);
                bundle.LoadAllAssetsAsync<T>().completed += operation =>
                {
                    var assetRequest = operation as AssetBundleRequest;
                    var assets = assetRequest?.allAssets;
                    DelBundleRef(bundleName, 1);
                    if (assets == null)
                    {
                        onFail?.Invoke();
                        return;
                    }
                    onSuccess?.Invoke(assets as IList<T>);
                };
            };
            _coroutineRunner.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName, onLoaded));
        }

        public void PreloadAsset(string assetPath)
        {
            if (_loadedAssets.TryGetCache(assetPath, out var asset) && asset)
            {
                return;
            }
            if (_loadingAssets.IsLoading(assetPath))
            {
                return;
            }
            var bundleName = _bundleIndex.GetBundleNameByAsset(assetPath);
            if (string.IsNullOrEmpty(bundleName))
            {
                return;
            }
            _loadPlan.AddPlan(bundleName, assetPath, typeof(Object));
            _loadingAssets.AddLoadingHandle(assetPath, null);
            GetAssetsBundleAsync(bundleName);
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