using UnityEngine;

namespace PowerCellStudio
{
    public class BundleRef : CacheRef<AssetBundle>
    {
        public BundleRef(AssetBundle asset) : base(asset)
        {
        }
    }
}