using System;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [Serializable]
    public class AssetPath<T> where T : Object
    {
        public string assetPath;

        public void LoadAsync(IAssetLoader assetLoader, Action<T> onSuccess)
        {
            if (assetLoader == null || string.IsNullOrEmpty(assetPath)) return;
            assetLoader.LoadAsync<T>(assetPath, onSuccess);
        }

        public LoaderYieldInstruction<T> LoadAsYieldInstruction(IAssetLoader assetLoader)
        {
            if (assetLoader == null || string.IsNullOrEmpty(assetPath)) return null;
            return assetLoader.LoadAsYieldInstruction<T>(assetPath);
        }
    }
}
