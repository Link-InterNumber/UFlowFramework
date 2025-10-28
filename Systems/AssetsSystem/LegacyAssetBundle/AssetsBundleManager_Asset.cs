using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private static Dictionary<string, LoaderYieldInstruction<Object>> _preloadHandles;
        private Dictionary<string, ScriptableAssetBundleData> _assetPathMap;

        private IEnumerator InitPathMap()
        {
            initProcess = 0f;
            initState = AssetInitState.InitModule;
            var path = Path.Combine(ConstSetting.BundleAssetConfigFolder, Path.GetFileNameWithoutExtension(ConstSetting.BundleAssetConfigName));
            var resourceRequest = Resources.LoadAsync<ScriptableAssetBundle>(path);
            yield return resourceRequest;
            var bundleDatas = resourceRequest.asset as ScriptableAssetBundle;
            if (bundleDatas == null)
            {
                AssetLog.LogError("AssetsBundleManager initialization failed");
                yield break;
            }
            _assetPathMap = new Dictionary<string, ScriptableAssetBundleData>();
            foreach (var scriptableAssetBundleData in bundleDatas.source)
            {
                if(scriptableAssetBundleData == null || string.IsNullOrEmpty(scriptableAssetBundleData.assetName)) continue;
                _assetPathMap.Add(scriptableAssetBundleData.assetName, scriptableAssetBundleData);
            }
            initProcess = 1f;
        }

        public void PreloadAsset(string path)
        {
            if (_preloadHandles == null) _preloadHandles = new Dictionary<string, LoaderYieldInstruction<Object>>();
            if (_preloadHandles.ContainsKey(path)) return;
            var loadAssetRequest = AssetUtils.GetLoadHandler<Object>(path);
            var bundleName = GetBundleNameByAsset(path);
            LoadAssetAsync<Object>(bundleName, path, loadAssetRequest);
            _preloadHandles.Add(path, loadAssetRequest);
        }

        public T LoadAsset<T>(string bundleName, string assetPath)
            where T : Object
        {
            if (_preloadHandles.TryGetValue(assetPath, out var handle) && handle.isDone)
            {
                _preloadHandles.Remove(assetPath);
                var asset = handle.asset as T;
                AssetUtils.ReleaseLoadHandler<T>(handle);
                return asset;
            }

            if (GetAssetBundle(bundleName, out var bundle))
            {
                if (bundle == null)
                {
                    return null;
                }
                var asset = bundle.LoadAsset<T>(assetPath);
                return asset;
            }
            return null;
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

            GetAssetsBundleAsync(bundleName, (bundle, bundleName) =>
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