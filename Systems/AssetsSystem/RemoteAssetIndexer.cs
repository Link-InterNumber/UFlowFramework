using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerCellStudio
{
    public class RemoteAssetIndexer
    {
        private Dictionary<string, BundleInfo> _remoteManifest;
        private Dictionary<string, BundleInfo> _clientManifest;
        private string _remotePath = "http://localhost:8000/StreamingAssets/";
        public static bool simulateRemoteBundleInEditor = false;

        // private readonly string _bundleFoldName;

        public RemoteAssetIndexer(string remotePath)
        {
            _remotePath = remotePath;
            // _bundleFoldName = bundleFoldName;
        }

        public IEnumerator Initialize(Action onDownloadStarted = null, Action<float> onDownloadProgress = null)
        {
            GetClientRemoteManifest();
            yield return GetServerRemoteManifest();
            yield return CheckRemoteAssets(onDownloadStarted, onDownloadProgress);
        }

        public bool IsBundleRemote(string bundleName)
        {
            return _clientManifest != null && _clientManifest.ContainsKey(bundleName);
        }

        public bool IsBundleNeedLoadFromRemote(string bundleName)
        {
#if UNITY_EDITOR
            if (!simulateRemoteBundleInEditor)
            {
                return false;
            }
#endif
            if (_remoteManifest == null || _remoteManifest.Count == 0) return false;
            if (_remoteManifest.TryGetValue(bundleName, out var remote))
            {
                if (!remote.isRemote) return false;
                if (_clientManifest.TryGetValue(bundleName, out var local))
                {
                    return local.md5 != remote.md5;
                }
                return true;
            }
            return false;
        }

        private void GetClientRemoteManifest()
        {
            var path = Path.Combine(Application.persistentDataPath, "remoteManifest.json");
            if (!File.Exists(path))
            {
                _clientManifest = new Dictionary<string, BundleInfo>();
                return;
            }
            var json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
            {
                _clientManifest = new Dictionary<string, BundleInfo>();
                return;
            }
            Dictionary<string, BundleInfo> result = null;
            try
            {
                var data = JsonConvert.DeserializeObject<RemoteManifest>(json);
                result = data?.GetMap() ?? null;
            }
            catch (Exception e)
            {
                AssetLogger.LogError(e);
            }
            finally
            {
                if (result == null) result = new Dictionary<string, BundleInfo>();
                _clientManifest = new Dictionary<string, BundleInfo>();
                foreach (var keyValuePair in result)
                {
                    var bundleName = keyValuePair.Key;
                    var bundlePath = Path.Combine(Application.persistentDataPath, bundleName);
                    if (File.Exists(bundlePath))
                    {
                        _clientManifest.Add(bundleName, keyValuePair.Value);
                    }
                }
                result = null;
            }
        }

        private IEnumerator GetServerRemoteManifest()
        {
#if UNITY_EDITOR
            var url = simulateRemoteBundleInEditor
                ? AssetUtils.BuildRemoteUrl("remoteManifest.json")
                : $"file://{Application.streamingAssetsPath}/remoteManifest.json";
#else
            var url = AssetUtils.BuildRemoteUrl("remoteManifest.json");
#endif
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 10;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var data = JsonConvert.DeserializeObject<RemoteManifest>(json);
                _remoteManifest = data?.GetMap() ?? null;
            }
            else
            {
#if !UNITY_EDITOR
                AssetLog.LogError("下载remoteManifest.json失败: " + request.error);
#endif
            }
            if (_remoteManifest == null) _remoteManifest = new Dictionary<string, BundleInfo>();
        }

        public void SaveRemoteManifest()
        {
            if (_clientManifest == null) return;
            RemoteManifest manifest = new RemoteManifest();
            foreach (var keyValue in _clientManifest)
            {
                manifest.bundles.Add(keyValue.Value);
            }
            // var folderName = "RemoteBundle";
            // var directory = Path.Combine(Application.streamingAssetsPath, folderName);
            // if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            // PlayerDataUtils.ReadJson<RemoteManifest>(manifest);
            string json = JsonConvert.SerializeObject(manifest);
            string savePath = Path.Combine(Application.persistentDataPath, "remoteManifest.json");
            var directory = Path.Combine(Application.persistentDataPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(savePath, json);
        }

        public void LoadRemoteBundle(string bundleName, Action<bool> onLoaded = null)
        {
            var url = AssetUtils.BuildRemoteUrl(bundleName);
            var path = Path.Combine(Application.persistentDataPath, bundleName);
            var webRequest = UnityWebRequest.Get(url);
            webRequest.downloadHandler = new DownloadHandlerFile(path);
            var operation = webRequest.SendWebRequest();
            operation.completed += _ =>
            {
                var success = webRequest.result == UnityWebRequest.Result.Success;
                webRequest.Dispose();
                if (!success)
                {
                    onLoaded?.Invoke(false);
                    return;
                }
                if (_remoteManifest.TryGetValue(bundleName, out var bundleInfo))
                {
                    _clientManifest[bundleName] = bundleInfo;
                }
                onLoaded?.Invoke(true);
            };
        }

        public IEnumerator LoadRemoteBundle(string bundleName, YieldInstructionCompletionSource<bool> handler = null)
        {
            var url = AssetUtils.BuildRemoteUrl(bundleName);
            var path = Path.Combine(Application.persistentDataPath, bundleName);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.downloadHandler = new DownloadHandlerFile(path);
                yield return webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    handler?.SetResult(false);
                    yield break;
                }
            }
            if (_remoteManifest.TryGetValue(bundleName, out var bundleInfo))
            {
                _clientManifest[bundleName] = bundleInfo;
            }
            handler?.SetResult(true);
        }

        private IEnumerator CheckRemoteAssets(Action onDownloadStarted, Action<float> onDownloadProgress)
        {
#if UNITY_EDITOR
            if (!simulateRemoteBundleInEditor)
            {
                yield break;
            }
#endif

            if (_remoteManifest == null || _clientManifest == null) yield break;
            var loadList = new List<string>();
            foreach (var keyValue in _remoteManifest)
            {
                var bundle = keyValue.Value;
                if (!bundle.isRemote) continue;
                if (_clientManifest.TryGetValue(bundle.name, out var localBundle))
                {
                    if (localBundle.md5 == bundle.md5 && localBundle.size == bundle.size)
                        continue;
                }
                loadList.Add(bundle.name);
            }
            if (loadList.Count == 0) yield break;
            onDownloadStarted?.Invoke();
            
            if (AssetUtils.loadMode == AssetUtils.LoadMode.AssetBundle)
            {
                // 下载新的AssetMap文件
                var indexFileUrl = AssetUtils.BuildRemoteUrl($"{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Index.bytes" );
                var dataFileUrl = AssetUtils.BuildRemoteUrl($"{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Data.bytes" );
                // 保存的本地位置
                var indexFilePath = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder, $"{ConstSetting.BundleAssetConfigName}Index.bytes" );
                var dataFilePath = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder, $"{ConstSetting.BundleAssetConfigName}Data.bytes" );
                var folder = Path.Combine(Application.persistentDataPath, ConstSetting.BundleAssetConfigFolder);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                var assetMapLoaded = true;
                using (UnityWebRequest webRequest = UnityWebRequest.Get(indexFileUrl))
                {
                    webRequest.downloadHandler = new DownloadHandlerFile(indexFilePath);
                    yield return webRequest.SendWebRequest();
                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        assetMapLoaded = false;
                    }
                }
                using (UnityWebRequest webRequest = UnityWebRequest.Get(dataFileUrl))
                {
                    webRequest.downloadHandler = new DownloadHandlerFile(dataFilePath);
                    yield return webRequest.SendWebRequest();
                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        assetMapLoaded = false;
                    }
                }
                if (assetMapLoaded)
                {
                    PlayerPrefs.SetInt(AssetBundleIndex.hasAssetBundleMapMovedKey, 1);
                    PlayerPrefs.Save();
                }
            }
            
            var token = new YieldInstructionCompletionSource<bool>();
            for (var i = 0; i < loadList.Count; i++)
            {
                var bundleName = loadList[i];
                yield return LoadRemoteBundle(bundleName, token);
                onDownloadProgress?.Invoke((i + 1f) / loadList.Count);
                if (!token.Result)
                {
                    AssetLogger.LogError($"下载远程Bundle失败: {bundleName}");
                }
                token.Reset();
            }
            SaveRemoteManifest();
        }
    }
}