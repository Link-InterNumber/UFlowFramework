using System;

namespace PowerCellStudio
{
    [Flags]
    public enum LoadState
    {
        Fail = 0,
        Begin = 1 << 0,
        LoadingBundle = 1 << 1,
        LoadingAsset = 1 << 2,
        End = 1 << 3,
        Loading = LoadingBundle | LoadingAsset
    }

    public class LoadSample 
    {
        public int runtimeFrameIndex;
        public int objectHashCode;
        public bool beginThisFrame;
        public string assetPath;
        public string assetBundleName;
        public LoadState loadState;
        public string[] assetDependencies;
        public string[] assetBundleDependencies;

        public void Reset()
        {
            runtimeFrameIndex = 0;
            objectHashCode = 0;
            beginThisFrame = false;
            assetPath = string.Empty;
            assetBundleName = string.Empty;
            loadState = LoadState.Begin;
            assetDependencies = null;
            assetBundleDependencies = null;
        }
    }
}