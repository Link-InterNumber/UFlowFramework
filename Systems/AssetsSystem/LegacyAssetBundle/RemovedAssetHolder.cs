using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    public class RemovedAssetHolder : MonoBehaviour
    {
        private struct RemovedAssetHolderData
        {
            public Object asset;
            public AssetBundle bundle;
            public float time;
        }

        private List<RemovedAssetHolderData> _removedAssetBuffer = new List<RemovedAssetHolderData>();
        private List<RemovedAssetHolderData> _removeAssetBox = new List<RemovedAssetHolderData>();

        public void Push(Object asset, AssetBundle bundle, float duration)
        {
            var data = new RemovedAssetHolderData
            {
                asset = asset,
                bundle = bundle,
                time = Time.unscaledTime + duration
            };
            _removedAssetBuffer.Add(data);
        }

        public void LateUpdate()
        {
            if (_removedAssetBuffer.Count == 0) return;
            var current = Time.unscaledTime;
            for (var i = 0; i < _removedAssetBuffer.Count;)
            {
                if (_removedAssetBuffer[i].time < current)
                {
                    _removeAssetBox.Add(_removedAssetBuffer[i]);
                    _removedAssetBuffer.RemoveAt(i);
                    continue;
                }
                i++;
            }
        }
    }
}