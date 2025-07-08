using System;
#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif
using UnityEngine;

namespace PowerCellStudio
{
    public interface ILoaderYieldInstruction : IDisposable
    {
        public bool isDone { get; }
    }
    
    public delegate void OnLoadCompleted<T>(T asset, string assetPath);
    // public delegate void OnLoadFailed(string assetPath);

    public class LoaderYieldInstruction<T> : CustomYieldInstruction, ILoaderYieldInstruction
        where T : class
    {
        public override bool keepWaiting => !isDone;
        public bool isDone { get; private set; }
        public T asset { get; private set; }
        private string _assetPath;
#if !UNITY_WEBGL
        private TaskCompletionSource<T> _taskCompletionSource;
#endif
        private event OnLoadCompleted<T> _onLoadCompleted;
        // public event OnLoadFailed onLoadFailed;

        public LoaderYieldInstruction(string assetPath)
        {
            _assetPath = assetPath;
            isDone = false;
            asset = null;
#if !UNITY_WEBGL
            _taskCompletionSource = new TaskCompletionSource<T>();
#endif
        }

#if !UNITY_WEBGL
        public Task<T> Task => _taskCompletionSource?.Task??null;
#endif

        public void OnLoadCompleted(OnLoadCompleted<T> callback)
        {
            if (isDone)
            {
                callback?.Invoke(asset, _assetPath);
                return;
            }
            _onLoadCompleted += callback;
        }

        public void SetAsset(T loadedAsset)
        {
            if (isDone) return;
            isDone = true;
            asset = loadedAsset;
            // if(asset == null)
            //     onLoadFailed?.Invoke(_assetPath);
            // else 
            _onLoadCompleted?.Invoke(loadedAsset, _assetPath);
#if !UNITY_WEBGL
            _taskCompletionSource?.SetResult(loadedAsset);
#endif
        }

        public void Dispose()
        {
            isDone = true;
            asset = null;
            // onLoadFailed = null;
            _onLoadCompleted = null;
            _assetPath = null;
#if !UNITY_WEBGL
            _taskCompletionSource = null;
#endif
        }
    }
}