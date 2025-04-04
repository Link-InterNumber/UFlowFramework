using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    /// <summary>
    /// ScriptableObject only asset reference.
    /// </summary>
    [Serializable]
    public class AssetReferenceScriptableObject: AssetReferenceT<ScriptableObject>
    {
        public AssetReferenceScriptableObject(string guid) : base(guid)
        {
        }
    }
}