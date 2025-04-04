#if UNITY_EDITOR

using System.Linq;
using UnityEditor;

namespace PowerCellStudio
{
    public class TexturePostprocessor : AssetPostprocessor
    {
        private static readonly string[] ASTC_6x6 = new[] { "" };
        private static readonly string[] ASTC_4x4 = new[] { "" };
        private static readonly string[] NO_COMPRESS = new[] {"/nocompress/", "/plugins/", "/editor/"};
        private void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing)
                return;
            var path = assetPath.ToLower();
            bool isNoCompress = NO_COMPRESS.Any(path.Contains);
            if (isNoCompress) return;
            bool isASTC6x6 = ASTC_6x6.Any(assetPath.Contains);
            bool isASTC_4x4 = ASTC_4x4.Any(assetPath.Contains);
            var platform = EditorUserBuildSettings.activeBuildTarget.ToString();
            var canSetASTC = TextureImporter.IsPlatformTextureFormatValid(TextureImporterType.Default,
                EditorUserBuildSettings.activeBuildTarget, TextureImporterFormat.ASTC_6x6);
            if (isASTC6x6 && canSetASTC)
            {
                TextureFormatSetter.SetPicFormat(assetPath, platform, TextureImporterFormat.ASTC_6x6, 2048, false, false);
            }
            else if (isASTC_4x4 && canSetASTC)
            {
                TextureFormatSetter.SetPicFormat(assetPath, platform, TextureImporterFormat.ASTC_4x4, 2048, false, false);
            }
            else
            {
                var canSetDXT5 = TextureImporter.IsPlatformTextureFormatValid(TextureImporterType.Default,
                    EditorUserBuildSettings.activeBuildTarget, TextureImporterFormat.DXT5);
                if(!canSetDXT5) return;
                TextureFormatSetter.SetPicFormat(assetPath, platform, TextureImporterFormat.DXT5, 2048, false, false);
            }
        }
    }
}
#endif
