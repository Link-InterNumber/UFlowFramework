using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    [Serializable]
    public class AssetReferenceAnimator: AssetReferenceT<RuntimeAnimatorController>
    {
        public AssetReferenceAnimator(string guid) : base(guid)
        {
        }
    }
}