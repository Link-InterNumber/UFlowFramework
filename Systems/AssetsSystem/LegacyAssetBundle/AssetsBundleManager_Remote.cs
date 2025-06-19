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

        private Dictionary<string, BundleInfo> GetClentRemoteManifest()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "remoteManifest.json");
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<RemoteManifest>(json);
            return data?.GetMap()??null;
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
                AssetLog.LogError("下载JSON失败: " + request.error);
                _remoteManifest = null;
            }
        }

        private void SaveRemoteManifest(Dictionary<string, BundleInfo> data)
        {
            if (data == null) return;
            RemoteManifest manifest = new RemoteManifest();
            foreach (var keyValue in data)
            {
                manifest.bundles.Add(keyValue.Value);
            }
            string json = JsonConvert.SerializeObject(manifest);
            string savePath = Path.Combine(Application.streamingAssetsPath, "remoteManifest.json");
            File.WriteAllText(savePath, json);
        }

        private IEnumerator CheckRemoteBundle()
        {
            yield return GetServerRemoteManifest();
            var clientManifest = GetClentRemoteManifest();
            if (_remoteManifest == null) yield break;
            var loadList = new List<string>();
            foreach (var keyValue in _remoteManifest)
            {
                var bundle = keyValue.Value;
                if (clientManifest.TryGetValue(bundle.name, out var localBundle))
                {
                    if (localBundle.md5 == bundle.md5 && localBundle.size == bundle.size)
                        continue;
                    loadList.Add(bundle.name);
                }
            }
            if (loadList.Count == 0) yield break;
            initState = AssetInitState.DownloadTheUpdateFile;
            initProcess = 0f;

            for (var i = 0; i < loadList.Count; i++)
            {
                var bundleName = loadList[i];
                var url = Path.Combine(_remotePath, bundleName);
                using var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);
                yield return webRequest;
                var bundleByte = webRequest.downloadHandler.data;
                yield return SaveBundleOnLocal(bundleName, bundleByte);
                Caching.ClearAllCachedVersions(bundleName);
                initProcess = i * 1f / loadList.Count;
            }
        }
    }
}