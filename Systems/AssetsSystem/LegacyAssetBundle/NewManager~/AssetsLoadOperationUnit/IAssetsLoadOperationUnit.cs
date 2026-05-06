using System.Collections;

namespace PowerCellStudio
{
    internal interface IAssetsLoadOperationUnit 
    {
        public IAssetsLoadOperationUnit next {get; set;}

        public LoaderYieldInstruction<T> Operation<T>(LoaderYieldInstruction<T> handler, NewAssetBundleManager manager, string assetPath, string bundleName,
            bool releaseBundleOnTime)
            where T : UnityEngine.Object;
    }
}