using System.Collections.Generic;
using UnityEngine;

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
        
        public bool TryGetLoadingHandle(string assetPath, out List<LoaderYieldInstruction<T>> handlerChain)
        {
            return _onloading.TryGetValue(assetPath, out handlerChain);
        }

        public bool AddLoadingHandle(string assetPath, LoaderYieldInstruction<T> handler)
        {
            if (_onloading.TryGetValue(assetPath, out var handlerChain))
            {
                handlerChain.Add(handler);
                return false;
            }
            else
            {
                handlerChain = new List<LoaderYieldInstruction<T>>();
                handlerChain.Add(handler);
                _onloading[assetPath] = handlerChain;
                return true;
            }
        }

        public int SetLoaded(string assetPath, T asset)
        {
            var refCount = 0;
            if (_onloading.TryGetValue(assetPath, out var handler))
            {
                refCount = handler.Count;
                foreach (var h in handler)
                {
                    h.SetAsset(asset);
                }
            }
            _onloading.Remove(assetPath);
            return refCount;
        }
    }
}