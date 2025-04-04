using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace PowerCellStudio
{
    /// <summary>
    /// AudioClip only asset reference.
    /// </summary>
    [Serializable]
    public class AssetReferenceAudioClip: AssetReferenceT<AudioClip>
    {
        public AssetReferenceAudioClip(string guid) : base(guid)
        {
        }
    }
}