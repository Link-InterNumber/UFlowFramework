using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public class AssetReferenceMaterial: AssetReferenceT<Material>
    {
        public AssetReferenceMaterial(string guid) : base(guid)
        {
        }
    }
}