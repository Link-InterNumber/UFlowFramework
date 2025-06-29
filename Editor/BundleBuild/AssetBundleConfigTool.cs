using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class AssetBundleConfigTool
    {
        [MenuItem("Build/AssetBundle/CreateMyData", false, 1)]
        public static void CreateAssetBundleConfig()
        {
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            if (!Directory.Exists(buildPath))
                return;
            // get manifest
            var manifestList = Directory.GetFiles(buildPath)
                .Where(o => Path.GetExtension(o) == ".manifest")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();

            //创建数据资源文件
            //泛型是继承自ScriptableObject的类
            var assetData = ScriptableObject.CreateInstance<ScriptableAssetBundle>();
            GetBundleAssetData(manifestList, buildPath, assetData);
            //前一步创建的资源只是存在内存中，现在要把它保存到本地
            //通过编辑器API，创建一个数据资源文件，第二个参数为资源文件在Assets目录下的路径
            var folder = Path.Combine("Assets", "Resources", ConstSetting.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var path = Path.Combine("Assets", "Resources", ConstSetting.BundleAssetConfigFolder,
                ConstSetting.BundleAssetConfigName);
            AssetDatabase.CreateAsset(assetData, path);
            //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
        }

        private static void GetBundleAssetData(List<string> manifestList, string foldPath, ScriptableAssetBundle assetData)
        {
            AssetBundle.UnloadAllAssetBundles(true);
            foreach (var item in manifestList)
            {
                var bundle = AssetBundle.LoadFromFile(Path.Combine(foldPath, item));
                if (string.IsNullOrEmpty(bundle.name))
                {
                    bundle.Unload(true);
                    continue;
                }

                var assets = bundle.GetAllAssetNames();
                foreach (var name in assets)
                {
                    if (Path.GetExtension(name) == "shader") continue;
                    assetData.source.Add(new ScriptableAssetBundleData()
                    {
                        // hashCode = name.GenHashCode(),
                        assetName = name,
                        assetBundle = bundle.name
                    });
                }

                bundle.Unload(true);
            }

            AssetBundle.UnloadAllAssetBundles(true);
        }
    }
}