using System;
using System.IO;
using System.Text;
// using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace PowerCellStudio
{
    [Serializable]
    public abstract class AssetsRef<T> : TypeRef, IBinaryData where T: Object
    {
        // public string bundleName;
        public string assetName;
        public string guid;

        public abstract LoaderYieldInstruction<T> Load(IAssetLoader assetLoader);

        // public abstract AssetReferenceT<T> GetAssetReference();

        public abstract void WriteData(BinaryWriter writer, Encoding encoding);

        public abstract void ReadData(BinaryReader reader, Encoding encoding);
    }
}