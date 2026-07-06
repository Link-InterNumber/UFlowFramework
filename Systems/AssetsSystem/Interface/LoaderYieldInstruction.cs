using UnityEngine;

#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    public delegate void OnLoadCompleted<T>(T asset, string assetPath);
    public delegate void OnLoadSuccess<T>(T asset);
    public delegate void OnLoadFailed();
    public delegate void OnDone<T>(LoaderYieldInstruction<T> instruction) where T : class;

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
        private event OnDone<T> _onDone;

        public LoaderYieldInstruction(string assetPath)
        {
            Reset(assetPath);
        }

        internal void Reset(string assetPath)
        {
            _assetPath = assetPath;
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

        /// <summary>
        /// 注册加载完成回调。会在所有回调后调用一次，如果资源已经加载完成，则会立即调用回调。
         /// <para>Register a callback for when loading is completed. The callback will be invoked once after all other callbacks. If the asset is already loaded, the callback will be invoked immediately.</para>
        /// </summary>
        /// <param name="callback"></param>
        internal void OnLoadCompleted(OnLoadCompleted<T> callback)
        {
            if (isDone)
            {
                callback?.Invoke(asset, _assetPath);
                return;
            }
            _onLoadCompleted += callback;
        }

        /// <summary>
        /// 注册加载成功回调。会在所有回调前调用一次，如果资源已经加载完成且加载成功，则会立即调用回调。
         /// <para>Register a callback for when loading is successful. The callback will be invoked once before all other callbacks. If the asset is already loaded and the loading is successful, the callback will be invoked immediately.</para>
        /// </summary>
        /// <param name="callback"></param>
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

        /// <summary>
        /// 注册加载失败回调。会在所有回调前调用一次，如果资源已经加载完成且加载失败，则会立即调用回调。
         /// <para>Register a callback for when loading fails. The callback will be invoked once before all other callbacks. If the asset is already loaded and the loading has failed, the callback will be invoked immediately.</para>
        /// </summary>
        /// <param name="callback"></param>
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

        /// <summary>
        /// 自动释放标志。如果有注册的完成回调，则为 true。
        /// <para>Auto-release flag. True if there is a registered completion callback.</para>
        /// </summary>
        public bool autoRelease => _onDone != null;

        // 不要直接调用这个方法，除非你知道自己在做什么。
        [System.Obsolete("This method is intended for internal use only. Do not call it directly unless you know what you're doing.")]
        internal void AddAutoReleaseHandle(OnDone<T> callback)
        {
            _onDone = callback;
        }

        /// <summary>
        /// 设置加载结果并触发回调。这个方法应该由资源加载系统调用，不应该由外部代码直接调用。
        /// <para>Set the loading result and trigger callbacks. This method should be called by the asset loading system and should not be called directly by external code.</para>
        /// </summary>
        /// <param name="loadedAsset"></param>
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
            _onDone?.Invoke(this);
            _onDone = null;
        }

        public void Dispose()
        {
            isDone = true;
            asset = null;
            _onLoadCompleted = null;
            _onLoadSuccess = null;
            _onLoadFailed = null;
            _assetPath = null;
            _taskCompletionSource?.SetResult(null);
            _taskCompletionSource = null;
            _onDone = null;
        }
    }
}