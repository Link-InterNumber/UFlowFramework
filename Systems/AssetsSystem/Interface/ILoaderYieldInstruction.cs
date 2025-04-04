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
        public event OnLoadCompleted<T> onLoadSuccess;
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

        public void SetAsset(T asset)
        {
            isDone = true;
            this.asset = asset;
            // if(asset == null)
            //     onLoadFailed?.Invoke(_assetPath);
            // else 
            onLoadSuccess?.Invoke(asset, _assetPath);
#if !UNITY_WEBGL
            _taskCompletionSource.SetResult(asset);
#endif
        }

        public void Dispose()
        {
            isDone = true;
            asset = null;
            // onLoadFailed = null;
            onLoadSuccess = null;
            _assetPath = null;
#if !UNITY_WEBGL
            _taskCompletionSource = null;
#endif
        }
    }
}