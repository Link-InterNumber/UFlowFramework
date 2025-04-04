using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [Serializable]
    public abstract class AssetsRef<T> : TypeRef where T: Object
    {
        // public string bundleName;
        public string assetName;
        public string guid;

        public abstract LoaderYieldInstruction<T> Load(IAssetLoader assetLoader);

        public abstract AssetReferenceT<T> GetAssetReference();

    }
}