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
        private string _assetPath;
        private TaskCompletionSource<T> _taskCompletionSource;
        private event OnLoadCompleted<T> _onLoadCompleted;
        private event OnLoadSuccess<T> _onLoadSuccess;
        private event OnLoadFailed _onLoadFailed;

        //         internal void Reset(string assetPath)
        //         {
        //             _assetPath = assetPath;
        //             isDone = false;
        //             asset = null;
        // #if !UNITY_WEBGL
        //             _taskCompletionSource = new TaskCompletionSource<T>();
        // #endif
        //         }

        public LoaderYieldInstruction(string assetPath)
        {
            Reset(assetPath);
        }

        public void Reset(string assetPath)
        {
            _assetPath = assetPath;
            isDone = false;
            asset = null;
            _onLoadCompleted = null;
            _onLoadSuccess = null;
            _onLoadFailed = null;
            _taskCompletionSource = new TaskCompletionSource<T>();
        }

        public Task<T> Task => _taskCompletionSource?.Task ?? null;

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
            _taskCompletionSource?.SetResult(loadedAsset);
            _onLoadCompleted?.Invoke(loadedAsset, _assetPath);
            _onLoadCompleted = null;
            _onLoadSuccess = null;
            _onLoadFailed = null;
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