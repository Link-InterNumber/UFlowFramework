using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public class AssetLoadingHolder<T>  where T : Object
    {
        private Dictionary<string, List<LoaderYieldInstruction<T>>>  _onloading;
        
        public AssetLoadingHolder()
        {
            _onloading = new Dictionary<string, List<LoaderYieldInstruction<T>>>();
        }

        public bool IsLoading(string assetPath)
        {
            return _onloading.ContainsKey(assetPath);
        }

        public void RemoveLoading(string assetPath)
        {
            if (_onloading.TryGetValue(assetPath, out var handler))
            {
                ListPool<LoaderYieldInstruction<T>>.Release(handler);
                _onloading.Remove(assetPath);
            }
        }
        
        public bool TryGetLoadingHandle(string assetPath, out List<LoaderYieldInstruction<T>> handlerChain)
        {
            return _onloading.TryGetValue(assetPath, out handlerChain);
        }

        public void AddLoadingHandle(string assetPath, LoaderYieldInstruction<T> handler)
        {
            if (handler == null)
            {
                AssetLog.LogError($"Trying to add null handler for asset {assetPath}");
                return;
            }
            if (_onloading.TryGetValue(assetPath, out var handlerChain))
            {
                handlerChain.Add(handler);
            }
            else
            {
                handlerChain = ListPool<LoaderYieldInstruction<T>>.Get();
                handlerChain.Add(handler);
                _onloading[assetPath] = handlerChain;
            }
        }

        public int SetLoaded(string assetPath, T asset)
        {
            var refCount = 0;
            if (_onloading.TryGetValue(assetPath, out var handler))
            {
                foreach (var h in handler)
                {
                    if (h == null || h.isDone) continue;
                    h.SetAsset(asset);
                    refCount++;
                }
                ListPool<LoaderYieldInstruction<T>>.Release(handler);
            }
            _onloading.Remove(assetPath);
            return refCount;
        }
    }
}