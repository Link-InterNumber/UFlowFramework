using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinkFrameWork.DesignPatterns;
using LinkFrameWork.MonoInstance;
using UnityEngine;
using UnityEngine.Events;
using Link.EditorScript.BundleBuilds;
using LinkFrameWork.Define;
using LinkFrameWork.Extentions;

namespace LinkFrameWork.AssetsManage
{
    public class AssetsBundleManager : SingletonBase<AssetsBundleManager>
    {
        /// <summary>
        /// 卸载bundle下限
        /// </summary>
        public static int DisposeRefLine = 0;

        /// <summary>
        /// 是否开启自动卸载
        /// </summary>
        public static bool EnableAutoUnload = true;

        private Dictionary<string, AssetsBundleRef> _loadedBundleDic = new Dictionary<string, AssetsBundleRef>();
        private List<string> _waitForLoadList = new List<string>();

        private AssetBundleManifest _bundleManifest;
        private List<ScriptableAssetBundleData> _assetBundleDatas;
        private bool _inited = false;

        // TODO
        private string _mainBundleName = "StandaloneWindows";

        private bool IsAssetsBundleLoaded(string bundleName)
        {
            return _loadedBundleDic.ContainsKey(bundleName);
        }

        public bool Init()
        {
            if (_inited) return true;
            GetBundleManifest();
            var path = Path.Combine(Consts.BundleAssetConfigFolder, Consts.BundleAssetConfigName.Split(".")[0]);
            if (!GetAssetBundle("init", out var bundle)) return false;
            _assetBundleDatas = Resources.Load<ScriptableAssetBundle>(path).source;
            _inited = true;
            return true;
        }


        public bool CheckWithID = true;

        public string GetBundleNameByAsset(string path)
        {
            if (!_inited) throw new Exception("AssetsBundleManager do not inited!!!");
            var lowerPath = path.ToLower();
            if (CheckWithID)
            {
                // -919671435
                var id = lowerPath.GenHashCode();
                var matched = _assetBundleDatas.FirstOrDefault(o => o.hashCode == id);
                return matched == default ? string.Empty : matched.assetBundle;
            }

            var nameMatched = _assetBundleDatas.FirstOrDefault(o => o.assetName == lowerPath);
            return nameMatched == default ? string.Empty : nameMatched.assetBundle;
        }

        #region BundleDependence

        private void GetBundleManifest()
        {
            if (_bundleManifest) return;
            var path = Path.Combine(Application.streamingAssetsPath, _mainBundleName);
            _waitForLoadList.Add(_mainBundleName);
            _loadedBundleDic.Remove(_mainBundleName);
            var loadedBundle = AssetBundle.LoadFromFile(path);
            _waitForLoadList.Remove(_mainBundleName);
            var abf = new AssetsBundleRef()
            {
                AutoDispose = false,
                Bundle = loadedBundle
            };
            _loadedBundleDic.Add(_mainBundleName, abf);
            abf.AddRef();
            if (!loadedBundle)
                throw new Exception($"MainBundle Name Error: {_mainBundleName}");
            _bundleManifest = loadedBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        }

        private string[] GetBundleDependencies(string bundleName)
        {
            GetBundleManifest();
            var dependencies = _bundleManifest.GetAllDependencies(bundleName);
            return dependencies ?? Array.Empty<string>();
        }

        private void LoadBundleDependence(string bundleName)
        {
            var dependencies = GetBundleDependencies(bundleName);
            foreach (var name in dependencies)
            {
                GetAssetBundle(name, out _);
            }
        }

        #endregion

        #region Load

        // 同步加载方案
        public bool GetAssetBundle(string bundleName, out AssetBundle loadedBundle, bool autoDispose = true)
        {
            if (_loadedBundleDic.TryGetValue(bundleName, out var loaded) && loaded.Alive)
            {
                loadedBundle = loaded.Bundle;
                loaded.AddRef();
            }
            else
            {
                loadedBundle = LoadAssetBundle(bundleName, autoDispose);
            }

            return loadedBundle;
        }

        private AssetBundle LoadAssetBundle(string bundleName, bool autoDispose)
        {
            if (_waitForLoadList.Contains(bundleName))
                return null;
            _waitForLoadList.Add(bundleName);
            _loadedBundleDic.Remove(bundleName);
            LoadBundleDependence(bundleName);
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            AssetBundle loadedBundle;
            try
            {
                loadedBundle = AssetBundle.LoadFromFile(path);
                var abf = new AssetsBundleRef()
                {
                    AutoDispose = autoDispose,
                    Bundle = loadedBundle
                };
                _loadedBundleDic.Add(loadedBundle.name, abf);
                abf.AddRef();
            }
            catch (Exception)
            {
                loadedBundle = null;
                Debug.LogError($"ERROR: bundleName={bundleName} do not exit");
            }

            _waitForLoadList.Remove(bundleName);
            return loadedBundle;
        }

        // 异步加载方案
        public void GetAssetsBundleAsync(string bundleName, UnityAction<AssetBundle> callBack, bool autoDispose = true)
        {
            if (_loadedBundleDic.TryGetValue(bundleName, out var loaded) && loaded.Alive)
            {
                callBack.Invoke(loaded.Bundle);
                loaded.AddRef();
            }
            else
            {
                if (_waitForLoadList.Contains(bundleName))
                    return;
                _waitForLoadList.Add(bundleName);
                _loadedBundleDic.Remove(bundleName);
                ApplicationManager.Instance.StartCoroutine(AsyncLoadAssetsBundleHandler(bundleName, callBack,
                    autoDispose));
            }
        }

        private IEnumerator AsyncLoadAssetsBundleHandler(string bundleName, UnityAction<AssetBundle> callBack,
            bool autoDispose = false)
        {
            LoadBundleDependence(bundleName);
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            var abcr = AssetBundle.LoadFromFileAsync(path);
            yield return abcr;
            _waitForLoadList.Remove(bundleName);
            if (!abcr.assetBundle)
            {
                Debug.LogError($"Bundle: {bundleName} Load Fail");
                yield break;
            }

            var abf = new AssetsBundleRef()
            {
                AutoDispose = autoDispose,
                Bundle = abcr.assetBundle
            };
            abf.AddRef();
            _loadedBundleDic.Add(abcr.assetBundle.name, abf);
            callBack?.Invoke(abcr.assetBundle);
        }

        #endregion


        #region Unload

        public void DeRefByBundleName(string bundleName)
        {
            if (_loadedBundleDic.TryGetValue(bundleName, out var loaded))
            {
                loaded.DeRef();
                // 依赖bundle.DeRef()
                var dependencies = GetBundleDependencies(bundleName);
                foreach (var name in dependencies)
                {
                    DeRefByBundleName(name);
                }
            }
        }

        public bool UnloadAssetsBundle(string bundleName)
        {
            if (!_loadedBundleDic.ContainsKey(bundleName)) return false;
            var loaded = _loadedBundleDic[bundleName];
            loaded.Bundle.Unload(false);
            _loadedBundleDic.Remove(bundleName);
            return true;
        }

        public void UnloadAllAssetsBundle()
        {
            AssetBundle.UnloadAllAssetBundles(false);
            _loadedBundleDic.Clear();
            _waitForLoadList.Clear();
            _bundleManifest = null;
        }

        #endregion
    }
}