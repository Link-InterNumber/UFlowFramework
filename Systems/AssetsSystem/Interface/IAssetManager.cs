using System;
using UnityEngine;

namespace PowerCellStudio
{
    /// <summary>
    /// 资源管理器接口，定义了资源加载、预加载、场景管理等相关操作。
    /// <para>Asset manager interface, defines resource loading, preloading, scene management, etc.</para>
    /// </summary>
    public interface IAssetManager
    {
        /// <summary>
        /// 初始化状态。
        /// <para>Initialization state.</para>
        /// </summary>
        AssetInitState initState { get; }

        /// <summary>
        /// 初始化进度（0~1）。
        /// <para>Initialization progress (0~1).</para>
        /// </summary>
        float initProcess { get; }

        /// <summary>
        /// 初始化资源管理器。
        /// <para>Initialize the asset manager.</para>
        /// </summary>
        /// <param name="coroutineRunner">用于运行协程的 MonoBehaviour 实例。<para>MonoBehaviour instance for running coroutines.</para></param>
        /// <param name="callBack">初始化完成后的回调。<para>Callback after initialization is complete.</para></param>
        void Init(MonoBehaviour coroutineRunner, Action callBack);

        /// <summary>
        /// 创建一个资源加载器实例。
        /// <para>Create an asset loader instance.</para>
        /// </summary>
        /// <param name="tag">加载器标签。<para>Loader tag.</para></param>
        /// <returns>资源加载器实例。<para>Asset loader instance.</para></returns>
        IAssetLoader SpawnLoader(string tag);

        /// <summary>
        /// 回收指定的资源加载器实例。
        /// <para>Despawn the specified asset loader instance.</para>
        /// </summary>
        /// <param name="loader">要回收的加载器。<para>Loader to despawn.</para></param>
        void DeSpawnLoader(IAssetLoader loader);

        /// <summary>
        /// 回收所有资源加载器实例。
        /// <para>Despawn all asset loader instances.</para>
        /// </summary>
        void DeSpawnAllLoader();

        /// <summary>
        /// 根据标签回收所有对应的资源加载器实例。
        /// <para>Despawn all asset loaders by tag.</para>
        /// </summary>
        /// <param name="tag">加载器标签。<para>Loader tag.</para></param>
        void DeSpawnLoaderByTag(string tag);

        /// <summary>
        /// 加载指定名称的场景。
        /// <para>Load the specified scene.</para>
        /// </summary>
        /// <param name="sceneName">场景名称。<para>Scene name.</para></param>
        /// <param name="onComplete">加载完成回调。<para>Callback when loading is complete.</para></param>
        /// <param name="unLoadOtherScene">是否卸载其他场景。<para>Whether to unload other scenes.</para></param>
        void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false);

        /// <summary>
        /// 卸载指定名称的场景。
        /// <para>Unload the specified scene.</para>
        /// </summary>
        /// <param name="name">场景名称。<para>Scene name.</para></param>
        void UnloadScene(string name);

        /// <summary>
        /// 预加载指定路径的资源。
        /// <para>Preload the asset at the specified path.</para>
        /// </summary>
        /// <param name="path">资源路径。<para>Asset path.</para></param>
        void PreloadAsset(string path);

        /// <summary>
        /// 按标签批量准备资源。
        /// <para>Prepare assets in batch by labels.</para>
        /// </summary>
        /// <param name="labels">资源标签数组。<para>Asset label array.</para></param>
        /// <param name="onComplete">准备完成回调。<para>Callback when preparation is complete.</para></param>
        /// <param name="isConcurrent">是否并发加载。<para>Whether to load concurrently.</para></param>
        /// <returns>准备处理句柄。<para>Prepare handler.</para></returns>
        PrepareHandler Prepare(string[] labels, Action onComplete, bool isConcurrent = false);

        /// <summary>
        /// 卸载准备好的资源。
        /// <para>Cancel asset preparation.</para>
        /// </summary>
        /// <param name="handler">准备处理句柄。<para>Prepare handler.</para></param>
        void Unprepare(PrepareHandler handler);

        /// <summary>
        /// 清除未引用的资源。
        /// <para>Clear unused asset.</para>
        /// </summary>
        void ClearUnusedAsset();
    }
}