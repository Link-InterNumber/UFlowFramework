using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class LoadingHolder<T>  where T : Object
    {
        private Dictionary<string, List<LoaderYieldInstruction<T>>>  _onloading;
        
        public LoadingHolder()
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

        public void AddLoadingHandle(string assetPath, LoaderYieldInstruction<T> handler)
        {
            if (_onloading.TryGetValue(assetPath, out var handlerChain))
            {
                handlerChain.Add(handler);
            }
            else
            {
                handlerChain = new List<LoaderYieldInstruction<T>>();
                handlerChain.Add(handler);
                _onloading[assetPath] = handlerChain;
            }
        }

        public void SetLoaded(string assetPath, T asset)
        {
            if (_onloading.TryGetValue(assetPath, out var handler))
            {
                foreach (var h in handler)
                {
                    h.SetAsset(asset);
                }
            }
            _onloading.Remove(assetPath);
        }
    }
}