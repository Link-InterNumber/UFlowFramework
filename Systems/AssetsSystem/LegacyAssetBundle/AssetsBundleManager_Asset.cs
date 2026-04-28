using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private static Dictionary<string, LoaderYieldInstruction<Object>> _preloadHandles;
        private ChunkDataQueryer<string, ScriptableAssetBundleData> _assetPathMap;

        private static readonly string _hasAssetBundleMapMovedKey = "hasAssetBundleMapMoved";
        private IEnumerator InitPathMap()
        {
            initProcess = 0f;
            var indexFilePath = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder, $"{ConstSetting.BundleAssetConfigName}Index.bytes" );
            var dataFilePath = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder, $"{ConstSetting.BundleAssetConfigName}Data.bytes" );
            // 创建目录
            var folder = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            // 初次启动，将streamingAssetsPath下的AssetBundleMap文件保存在Application.persistentDataPath
            var hasAssetBundleMapMoved = PlayerPrefs.GetInt(_hasAssetBundleMapMovedKey, 0) > 0;
            if (!hasAssetBundleMapMoved)
            {
                var indexFilePathV0 =$"{Application.streamingAssetsPath}/{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Index.bytes";
                var dataFilePathV0 =$"{Application.streamingAssetsPath}/{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Data.bytes";
#if UNITY_ANDROID
                using (UnityWebRequest request = UnityWebRequest.Get("file://" + indexFilePathV0))
                {
                    request.downloadHandler = new DownloadHandlerFile(indexFilePath);
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        AssetLog.LogError(
                            "AssetsBundleManager initialization failed for coping AssetBundleMap file to persistentDataPath");
                        yield break;
                    }
                }
                using (UnityWebRequest request = UnityWebRequest.Get("file://" + dataFilePathV0))
                {
                    request.downloadHandler = new DownloadHandlerFile(dataFilePath);
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        AssetLog.LogError(
                            "AssetsBundleManager initialization failed for coping AssetBundleMap file to persistentDataPath");
                        yield break;
                    }
                }
#else
                File.WriteAllBytes(indexFilePath, File.ReadAllBytes(indexFilePathV0));
                File.WriteAllBytes(dataFilePath, File.ReadAllBytes(dataFilePathV0));
#endif
                PlayerPrefs.SetInt(_hasAssetBundleMapMovedKey, 1);
                PlayerPrefs.Save();
            }
            
            // 从本地Application.persistentDataPath目录加载分包配置文件
            _assetPathMap = new ChunkDataQueryer<string, ScriptableAssetBundleData>();
            yield return _assetPathMap.PrepareYieldInstruction(indexFilePath, dataFilePath, data => data.assetName);
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
                var preloadAsset = handle.asset as T;
                AssetUtils.ReleaseLoadHandler<T>(handle);
                return preloadAsset;
            }

            if (!GetAssetBundle(bundleName, out var bundle)) 
                return null;
            if (bundle == null)
            {
                return null;
            }

            if (AssetUtils.TryGetSubAssetName(assetPath, out var mainPath, out var subAssetName))
            {
                var assets = bundle.LoadAssetWithSubAssets<T>(mainPath);
                if (assets == null)
                    return null;

                foreach (var a in assets)
                {
                    if (a == null || a.name != subAssetName || a is not T matched) 
                        continue;
                    return matched;
                }

                return null;
            }

            var asset = bundle.LoadAsset<T>(assetPath);
            return asset;
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

            if (AssetUtils.TryGetSubAssetName(assetPath, out var mainPath, out var subAssetName))
            {
                var assetRequest = bundle.LoadAssetWithSubAssetsAsync<T>(mainPath);
                assetRequest.completed += operation =>
                {
                    var operationHandle = operation as AssetBundleRequest;
                    if (operationHandle == null)
                    {
                        loadAssetRequest.SetAsset(null);
                        return;
                    }

                    var assets = operationHandle.allAssets as T[];
                    if (assets == null)
                    {
                        loadAssetRequest.SetAsset(null);
                        return;
                    }
                    foreach (var a in assets)
                    {
                        if (a == null) continue;
                        if (a.name == subAssetName && a is T matched)
                        {
                            loadAssetRequest.SetAsset(matched);
                            return;
                        }
                    }

                    loadAssetRequest.SetAsset(null);
                };
            }
            else
            {
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