using System;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IAssetManager //<T> where T : IAssetLoader
    {
        AssetInitState initState {get;}

        float initProcess {get;}

        public void Init(MonoBehaviour coroutineRunner, Action callBack);
        
        public IAssetLoader SpawnLoader(string tag);
        
        public void DeSpawnLoader(IAssetLoader loader);
        
        public void DeSpawnAllLoader();
        
        public void DeSpawnLoaderByTag(string tag);

        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false);

        public void UnloadScene(string name);
    }
}