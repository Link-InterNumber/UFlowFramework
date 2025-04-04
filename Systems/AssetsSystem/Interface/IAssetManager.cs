using System;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IAssetManager<T> where T : IAssetLoader
    {
        public void Init(MonoBehaviour coroutineRunner, Action callBack);
        
        public T SpawnLoader(string tag);
        
        public void DeSpawnLoader(T loader);
        
        public void DeSpawnAllLoader();
        
        public void DeSpawnLoaderByTag(string tag);

        public void LoadScene(string sceneName, Action onComplete, bool unLoadOtherScene = false);

        public void UnloadScene(string name);
    }
}