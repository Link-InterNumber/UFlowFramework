using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class RemovedAssetHolder : MonoBehaviour
    {
        private struct RemovedAssetHolderData
        {
            public string bundleName;
            public AssetBundle bundle;
            public float time;
        }

        private List<RemovedAssetHolderData> _removedAssetBuffer = new List<RemovedAssetHolderData>();
        private Dictionary<string, AssetBundle> _map = new Dictionary<string, AssetBundle>();

        public void Push(string bundleName, AssetBundle bundle, float duration)
        {
            var data = new RemovedAssetHolderData
            {
                bundleName = bundleName,
                bundle = bundle,
                time = Time.unscaledTime + duration
            };
            _removedAssetBuffer.Add(data);
            _map[bundleName] = bundle;
        }

        public bool TryGetBundle(string bundleName, out AssetBundle bundle)
        {
            var result = _map.TryGetValue(bundleName, out bundle);
            if (result)
            {
                _map.Remove(bundleName);
                for (var i = 0; i < _removedAssetBuffer.Count; i++)
                {
                    if (_removedAssetBuffer[i].bundleName == bundleName)
                    {
                        var data = _removedAssetBuffer[i];
                        data.time = 0;
                        _removedAssetBuffer[i] = data;
                        break;
                    }
                }
            }
            return result;
        }

        public void LateUpdate()
        {
            if (_removedAssetBuffer.Count == 0) return;
            var current = Time.unscaledTime;
            for (var i = 0; i < _removedAssetBuffer.Count;)
            {
                if (_removedAssetBuffer[i].time == 0)
                {
                    _removedAssetBuffer.RemoveAt(i);
                    continue;
                }
                if (_removedAssetBuffer[i].time < current)
                {
                    _map.Remove(_removedAssetBuffer[i].bundleName);
                    UnloadData(_removedAssetBuffer[i]);
                    _removedAssetBuffer.RemoveAt(i);
                    continue;
                }
                i++;
            }
        }

        private void UnloadData(RemovedAssetHolderData assetHolderData)
        {
            // if (assetHolderData.asset)
            // {
            //     Resources.UnloadAsset(assetHolderData.asset);
            // }

            if (assetHolderData.bundle)
            {
                assetHolderData.bundle.UnloadAsync(false);
            }

            // assetHolderData.asset = null;
            assetHolderData.bundle = null;
            assetHolderData.bundleName = null;
        }
    }
}