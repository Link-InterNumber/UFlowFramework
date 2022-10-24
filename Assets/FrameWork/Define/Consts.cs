using System.IO;

namespace LinkFrameWork.Define
{
    public class Consts
    {
        /// <summary>
        /// 路径分隔符
        /// </summary>
        public static readonly char[] PathSeparator = new char[]
            { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        /// <summary>
        /// 分包配置文件夹
        /// </summary>
        public static readonly string BundleAssetConfigFolder = "Assets/Res/AssetBundleData";

        /// <summary>
        /// 分包配置文件名
        /// </summary>
        public static readonly string BundleAssetConfigName = "AssetBundleData.asset";
    }
}