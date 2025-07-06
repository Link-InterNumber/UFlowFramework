using UnityEditor;

namespace PowerCellStudio
{
    public class AssetsBundleBuildUtils
    {
        public static string GetBuildFoldName(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                    return "StandaloneOSX";
                case BuildTarget.StandaloneWindows:
                    return "StandaloneWindows";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.StandaloneWindows64:
                    return "StandaloneWindows";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneLinux64:
                    return "StandaloneLinux";
                case BuildTarget.PS4:
                    return "PS4";
                case BuildTarget.PS5:
                    return "PS5";
                case BuildTarget.tvOS:
                    return "tvOS";
                case BuildTarget.Switch:
                    return "Switch";
                case BuildTarget.XboxOne:
                case BuildTarget.GameCoreXboxOne:
                    return "XboxOne";
                case BuildTarget.GameCoreXboxSeries:
                    return "XboxSeries";
                default:
                    return "StreamingAssets";
            }
        }

    }
}