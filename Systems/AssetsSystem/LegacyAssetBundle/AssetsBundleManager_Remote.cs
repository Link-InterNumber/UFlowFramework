using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        private Dictionary<string, BundleInfo> _remoteManifest;
        private Dictionary<string, BundleInfo> _clientManifest;
        private string _remotePath = "http://localhost:8000/StreamingAssets/";

        private void GetClentRemoteManifest()
        {
            var path = Path.Combine(Application.persistentDataPath, "RemoteBundle", "remoteManifest.json");
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            Dictionary<string, BundleInfo> result = null;
            try
            {
                var data = JsonConvert.DeserializeObject<RemoteManifest>(json);
                result = data?.GetMap()??null;
            }
            catch (Exception e)
            {
                AssetLog.LogError(e);
            }
            finally
            {
                if (result == null) result = new Dictionary<string, BundleInfo>();
            }
            _clientManifest = result;
        }
        
        private IEnumerator GetServerRemoteManifest()
        {
            var url = Path.Combine(_remotePath, "remoteManifest.json");
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                var data = JsonConvert.DeserializeObject<RemoteManifest>(json);
                _remoteManifest =  data?.GetMap()??null;
            }
            else
            {
                AssetLog.LogError("下载remoteManifest.json失败: " + request.error);
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
            var folderName = "RemoteBundle";
            var directory = Path.Combine(Application.persistentDataPath, folderName);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            // PlayerDataUtils.ReadJson<RemoteManifest>(manifest);
            string json = JsonConvert.SerializeObject(manifest);
            string savePath = Path.Combine(Application.persistentDataPath, folderName, "remoteManifest.json");
            File.WriteAllText(savePath, json);
        }

        private IEnumerator LoadRemoteBundle(string bundleName, LoaderYieldInstruction<AssetBundle> handler = null)
        {
            var url = Path.Combine(_remotePath, bundleName);
            var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);
            yield return webRequest.SendWebRequest();;
            var bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
            if (!bundle) 
            {
                webRequest.Dispose();
                if (handler == null) yield break;
                handler.SetAsset(null);
                yield break;
            }
            var bundleByte = webRequest.downloadHandler.data;
            webRequest.Dispose();
            yield return SaveBundleOnLocal(bundleName, bundleByte);
            if (_remoteManifest.TryGetValue(bundleName, out var bundleInfo))
            {
                _clientManifest[bundleName] = bundleInfo;
            }
            if (handler == null) yield break;
            handler.SetAsset(bundle);
        }

        private IEnumerator CheckRemoteBundle()
        {
            if (_remoteManifest == null || _clientManifest == null) yield break;
            var loadList = new List<string>();
            foreach (var keyValue in _remoteManifest)
            {
                var bundle = keyValue.Value;
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

            for (var i = 0; i < loadList.Count; i++)
            {
                var bundleName = loadList[i];
                yield return LoadRemoteBundle(bundleName);
                Caching.ClearAllCachedVersions(bundleName);
                initProcess = i * 1f / loadList.Count;
            }
            SaveRemoteManifest(_clientManifest);
        }
    }
}