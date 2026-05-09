using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public partial class AssetsBundleManager
    {
        public IAssetLoader CreateLoader()
        {
            return new BundleAssetLoader(this);
        }
    }
}