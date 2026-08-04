using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class BundleLoadingHolder
    {
        private Dictionary<string, LoaderYieldInstruction<AssetBundle>>  _onloading; 

        public BundleLoadingHolder()
        {
            _onloading = new Dictionary<string, LoaderYieldInstruction<AssetBundle>>();
        }

        public bool IsLoading(string bundleName)
        {
            return _onloading.ContainsKey(bundleName);
        }

        // public bool TryGetLoadingHandle(string bundleName, out LoaderYieldInstruction<AssetBundle> handler)
        // {
        //     return _onloading.TryGetValue(bundleName, out handler);
        // }

        public LoaderYieldInstruction<AssetBundle> AddLoadingHandle(string bundleName, OnLoadCompleted<AssetBundle> handler)
        {
            if (_onloading.TryGetValue(bundleName, out var existingHandler))
            {
                if (handler != null) existingHandler.OnLoadCompleted(handler);
                return existingHandler;
            }
            var yieldInstruction = AssetUtils.GetLoadHandler<AssetBundle>(bundleName);
            if (handler != null) yieldInstruction.OnLoadCompleted(handler);
            _onloading.Add(bundleName, yieldInstruction);
            return yieldInstruction;
        }

        public void Clear()
        {
            foreach (var loaderYieldInstruction in _onloading.Values)
            {
                AssetUtils.ReleaseLoadHandler<AssetBundle>(loaderYieldInstruction);
            }
            _onloading.Clear();
        }

        public void SetLoaded(string bundleName, AssetBundle bundle)
        {
            if (_onloading.TryGetValue(bundleName, out var handler))
            {
                handler?.SetAsset(bundle);
                AssetUtils.ReleaseLoadHandler<AssetBundle>(handler);
            }
            else
            {
                AssetLogger.LogError($"Bundle {bundleName} is not loading");
            }
            _onloading.Remove(bundleName);
        }
    }
}