using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PowerCellStudio
{
    /// <summary>
    /// 使用Unity Resources接口实现的资源管理器。
    /// Resource manager implemented with Unity Resources API.
    /// </summary>
    public class ResourceManager : MonoBehaviour, IAssetManager
    {
        public AssetInitState initState { get; private set; } = AssetInitState.Complete;
        public float initProcess { get; private set; } = 0f;

        private MonoBehaviour coroutineRunner;

        public void Init(MonoBehaviour coroutineRunner, Action callBack)
        {
            this.coroutineRunner = coroutineRunner;
            initState = AssetInitState.InitModule;
            initProcess = 0.5f;
            // Resources不需要复杂初始化，直接完成
            initState = AssetInitState.Complete;
            initProcess = 1f;
            callBack?.Invoke();
        }

        public IAssetLoader CreateLoader()
        {
            return new ResourceAssetLoader();
        }

        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false)
        {
            coroutineRunner.StartCoroutine(LoadSceneCoroutine(sceneName, onComplete, unLoadOtherScene));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, Action onComplete, bool unLoadOtherScene)
        {
            if (unLoadOtherScene)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name != sceneName)
                    {
                        yield return SceneManager.UnloadSceneAsync(scene);
                    }
                }
            }
            var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return async;
            onComplete?.Invoke();
        }

        public void UnloadScene(string name)
        {
            SceneManager.UnloadSceneAsync(name);
        }

        public void ClearUnusedAsset()
        {
            Resources.UnloadUnusedAssets();
        }

        public void PreloadAsset(string address)
        {
            Resources.LoadAsync(address);
        }
    }
}