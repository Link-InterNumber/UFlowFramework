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

        public bool Release(string address);

        // public void Concat(IAssetLoader other);
        
        public bool IsLoading(string address);

        public bool IsAnyLoading();
        
        public void LoadAsync<T>(string address, OnLoadSuccess<T> onSuccess, OnLoadFailed onFail = null) where T : UnityEngine.Object;

        public Task<T> LoadTask<T>(string address) where T : UnityEngine.Object;

        public LoaderYieldInstruction<T> LoadAsYieldInstruction<T>(string address) where T : UnityEngine.Object;

        public void AsyncLoadNInstantiate(string address, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null);
        
        public void AsyncLoadNInstantiate(string address, Transform parent, OnLoadSuccess<GameObject> onSuccess, OnLoadFailed onFail = null);

        public void LoadAllAsync<T>(string label, OnLoadSuccess<IList<T>> onSuccess, OnLoadFailed onFail = null) where T : UnityEngine.Object;
    }
}