using UnityEngine;

#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    public delegate void OnLoadCompleted<T>(T asset, string assetPath);
    public delegate void OnLoadSuccess<T>(T asset);
    public delegate void OnLoadFailed();

    public class LoaderYieldInstruction<T> : CustomYieldInstruction, ILoaderYieldInstruction
        where T : class
    {
        public override bool keepWaiting => !isDone;
        
        public bool isDone { get; private set; }
        public T asset { get; private set; }
        public bool canceled { get; private set; }
        
        private string _assetPath;
        private TaskCompletionSource<T> _taskCompletionSource;
        private event OnLoadCompleted<T> _onLoadCompleted;
        private event OnLoadSuccess<T> _onLoadSuccess;
        private event OnLoadFailed _onLoadFailed;

        public LoaderYieldInstruction(string assetPath)
        {
            Reset(assetPath);
        }

        public void Reset(string assetPath)
        {
            _assetPath = assetPath;
            isDone = false;
            canceled  = false;
            asset = null;
            _onLoadCompleted = null;
            _onLoadSuccess = null;
            _onLoadFailed = null;
        }

        public Task<T> Task
        {
            get
            {
                if (isDone)
                    return System.Threading.Tasks.Task.FromResult(asset);

                return (_taskCompletionSource ??= new TaskCompletionSource<T>()).Task;
            }
        }

        internal void OnLoadCompleted(OnLoadCompleted<T> callback)
        {
            if (isDone)
            {
                callback?.Invoke(asset, _assetPath);
                return;
            }
            _onLoadCompleted += callback;
        }

        public void OnLoadSuccess(OnLoadSuccess<T> callback)
        {
            if (isDone)
            {
                if (asset != null)
                    callback?.Invoke(asset);
                return;
            }
            _onLoadSuccess += callback;
        }

        public void OnLoadFailed(OnLoadFailed callback)
        {
            if (isDone)
            {
                if (asset == null)
                    callback?.Invoke();
                return;
            }
            _onLoadFailed += callback;
        }

        public void SetAsset(T loadedAsset)
        {
            if (isDone) return;
            isDone = true;
            asset = loadedAsset;
            if (asset == null)
                _onLoadFailed?.Invoke();
            else
                _onLoadSuccess?.Invoke(loadedAsset);
            _onLoadCompleted?.Invoke(loadedAsset, _assetPath);
            _taskCompletionSource?.SetResult(loadedAsset);
            _onLoadCompleted = null;
            _onLoadSuccess = null;
            _onLoadFailed = null;
            _taskCompletionSource = null;
        }

        public void SetCancelled()
        {
            if (isDone || canceled) return;
            isDone = true;
            canceled =  true;
            _taskCompletionSource?.SetCanceled();
        }

        public void Dispose()
        {
            isDone = true;
            asset = null;
            _onLoadCompleted = null;
            _onLoadSuccess = null;
            _onLoadFailed = null;
            _assetPath = null;
            _taskCompletionSource = null;
        }
    }
}