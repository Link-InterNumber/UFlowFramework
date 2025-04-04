using System;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerCellStudio
{
    public interface IAssetLoader 
    {
        public long index { get;}
        public bool spawned { get; }
        public string tag { get; set; }

        public void Init();
        
        public void Deinit();

        public bool Release(string address);
        
        public bool IsLoading(string address);
        
        public void LoadAsync<T>(string address, Action<T> onSuccess, Action onFail = null) where T : UnityEngine.Object;

        public Task<T> LoadTask<T>(string address) where T : UnityEngine.Object;

        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : UnityEngine.Object;

        public void AsyncLoadNInstantiate(string address, Action<GameObject> onSuccess, Action onFail = null);
        
        public void AsyncLoadNInstantiate(string address, Transform parent, Action<GameObject> onSuccess, Action onFail = null);
    }
}