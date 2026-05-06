using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
#if !UNITY_WEBGL
using Microsoft.Xbox.Services.Client;
#endif

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private Dictionary<string, BundleInfo> _remoteManifest;
        private Dictionary<string, BundleInfo> _clientManifest;
        private string _remotePath = "http://localhost:8000/StreamingAssets/";
        public static bool simulateRemoteBundleInEditor = false;

        private string BuildRemoteUrl(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            // 基础防注入：只允许文件名，不允许路径分隔符
            if (string.IsNullOrEmpty(safeName) || safeName != fileName) return null;
            return $"{_remotePath.TrimEnd('/')}/{Uri.EscapeDataString(safeName)}";
        }

        private bool IsBundleNeedLoadFromRemote(string bundleName)
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
            var path = Path.Combine(Application.persistentDataPath, _bundleFoldName, "remoteManifest.json");
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
                AssetLog.LogError(e);
            }
            finally
            {
                if (result == null) result = new Dictionary<string, BundleInfo>();
                _clientManifest = new Dictionary<string, BundleInfo>();
                foreach (var keyValuePair in result)
                {
                    var bundleName = keyValuePair.Key;
                    var bundlePath = Path.Combine(Application.persistentDataPath, _bundleFoldName, bundleName);
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
                ? BuildRemoteUrl("remoteManifest.json")
                : $"file://{Application.streamingAssetsPath}/remoteManifest.json";
#else
            var url = BuildRemoteUrl("remoteManifest.json");
#endif
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 30;
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

        private void SaveRemoteManifest(Dictionary<string, BundleInfo> data)
        {
            if (data == null) return;
            RemoteManifest manifest = new RemoteManifest();
            foreach (var keyValue in data)
            {
                manifest.bundles.Add(keyValue.Value);
            }
            // var folderName = "RemoteBundle";
            // var directory = Path.Combine(Application.streamingAssetsPath, folderName);
            // if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            // PlayerDataUtils.ReadJson<RemoteManifest>(manifest);
            string json = JsonConvert.SerializeObject(manifest);
            string savePath = Path.Combine(Application.persistentDataPath, _bundleFoldName, "remoteManifest.json");
            var directory = Path.Combine(Application.persistentDataPath, _bundleFoldName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(savePath, json);
        }

        private IEnumerator LoadRemoteBundle(string bundleName, YieldInstructionCompletionSource<bool> handler = null)
        {
            var url = BuildRemoteUrl(bundleName);
            var path = Path.Combine(Application.persistentDataPath, _bundleFoldName, bundleName);
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

        private IEnumerator CheckRemoteBundle()
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
            initState = AssetInitState.DownloadTheUpdateFile;
            initProcess = 0f;
            
            // 下载新的AssetMap文件
            var indexFileUrl = BuildRemoteUrl($"{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Index.bytes" );
            var dataFileUrl = BuildRemoteUrl($"{ConstSetting.BundleAssetConfigFolder}/{ConstSetting.BundleAssetConfigName}Data.bytes" );
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
                    assetMapLoaded = false;
            }
            using (UnityWebRequest webRequest = UnityWebRequest.Get(dataFileUrl))
            {
                webRequest.downloadHandler = new DownloadHandlerFile(dataFilePath);
                yield return webRequest.SendWebRequest();
                if (webRequest.result != UnityWebRequest.Result.Success)
                    assetMapLoaded = false;
            }

            if (assetMapLoaded)
            {
                PlayerPrefs.SetInt(AssetBundleIndex.hasAssetBundleMapMovedKey, 1);
                PlayerPrefs.Save();
            }
            
            var token = new YieldInstructionCompletionSource<bool>();
            for (var i = 0; i < loadList.Count; i++)
            {
                var bundleName = loadList[i];
                yield return LoadRemoteBundle(bundleName, token);
                initProcess = i * 1f / loadList.Count;
                if (!token.Result)
                {
                    AssetLog.LogError($"下载远程Bundle失败: {bundleName}");
                }
                token.Reset();
            }
            SaveRemoteManifest(_clientManifest);
        }
    }
}