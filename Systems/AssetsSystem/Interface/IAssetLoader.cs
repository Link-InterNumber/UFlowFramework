using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IAssetLoader 
    {
        public int index { get;}
        public bool spawned { get; }
        public string tag { get; set; }

        public void Init();
        
        public void Deinit();

        /// <summary>
        /// 释放资源
        /// Release an asset.
        /// </summary>
        /// <param name="address">资源地址。Asset address.</param>
        /// <returns>是否成功释放。Whether the asset was released successfully.</returns>
        public bool Release(string address);

        // public void Concat(IAssetLoader other);
        
        /// <summary>
        /// 是否正在加载资源
        /// Determine whether the specified asset is currently loading.
        /// </summary>
        /// <param name="address">资源地址。Asset address.</param>
        /// <returns>是否正在加载。Whether the asset is currently loading.</returns>
        public bool IsLoading(string address);

        /// <summary>
        /// 是否有正在加载的资源
        /// Determine whether any asset is currently loading.
        /// </summary>
        /// <returns>是否存在正在加载的资源。Whether any asset is currently loading.</returns>
        public bool IsAnyLoading();
        
        /// <summary>
        /// 异步加载资源
        /// Load an asset asynchronously.
        /// </summary>
        /// <typeparam name="T">资源类型。Asset type.</typeparam>
        /// <param name="address">资源地址。Asset address.</param>
        /// <param name="onSuccess">加载成功回调。Callback invoked when loading succeeds.</param>
        /// <param name="onFail">加载失败回调。Callback invoked when loading fails.</param>
        public void LoadAsync<T>(string address, OnLoadSuccess<T> onSuccess, OnLoadFailed onFail = null) where T : UnityEngine.Object;

#if !UNITY_WEBGL
        /// <summary>
        /// 以Task异步加载资源并实例化
        /// Load an asset asynchronously as a task.
        /// </summary>
        /// <typeparam name="T">资源类型。Asset type.</typeparam>
        /// <param name="address">资源地址。Asset address.</param>
        public Task<T> LoadTask<T>(string address) where T : UnityEngine.Object;
#endif

        /// <summary>
        /// 以YieldInstruction异步加载资源
        /// Load an asset asynchronously as a yield instruction.
        /// </summary>
        /// <typeparam name="T">资源类型。Asset type.</typeparam>
        /// <param name="address">资源地址。Asset address.</param>
        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载资源并实例化，适用于GameObject等需要实例化的资源
        /// Load and instantiate an asset asynchronously, for assets such as GameObject.
        /// </summary>
        /// <param name="address">资源地址。Asset address.</param>
        /// <param name="onSuccess">加载成功回调。Callback invoked when loading succeeds.</param>
        /// <param name="onFail">加载失败回调。Callback invoked when loading fails.</param>
        public void AsyncLoadNInstantiate(string address, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null);
        
        /// <summary>
        /// 异步加载资源并实例化，适用于GameObject等需要实例化的资源
        /// Load and instantiate an asset asynchronously, for assets such as GameObject.
        /// </summary>
        /// <param name="address">资源地址。Asset address.</param>
        /// <param name="parent">父对象。Parent transform.</param>
        /// <param name="onSuccess">加载成功回调。Callback invoked when loading succeeds.</param>
        /// <param name="onFail">加载失败回调。Callback invoked when loading fails.</param>
        public void AsyncLoadNInstantiate(string address, Transform parent, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null);

        /// <summary>
        /// 通过标签批量加载资源
        /// Load all assets with the specified label asynchronously.
        /// </summary>
        /// <typeparam name="T">资源类型。Asset type.</typeparam>
        /// <param name="label">标签。Asset label.</param>
        /// <param name="onSuccess">加载成功回调。Callback invoked when loading succeeds.</param>
        /// <param name="onFail">加载失败回调。Callback invoked when loading fails.</param>
        public void LoadAllAsync<T>(string label, OnLoadSuccess<IList<T>> onSuccess, OnLoadFailed onFail = null) where T : UnityEngine.Object;

        #if UNITY_EDITOR
        /// <summary>
        /// Editor模式下获取所有已加载的资源地址
        /// Get all loaded asset addresses in Editor mode.
        /// </summary>
        /// <returns>所有已加载的资源地址。All loaded asset addresses.</returns>
        public IEnumerable<string> GetAllLoadedAssets();
        #endif
    }
}