using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinkFrameWork.Define;
using LinkFrameWork.Extentions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Link.EditorScript.BundleBuilds
{
    public class EditorBundleBuild
    {
        [MenuItem("ScritableObject/CreateMyData")]
        public static void SaveAssetBundleData()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
                return;
            // get manifest
            var manifestList = Directory.GetFiles(Application.streamingAssetsPath)
                .Where(o => Path.GetExtension(o) == ".manifest")
                .Select(o => Path.GetFileNameWithoutExtension(o))
                .ToList();

            //创建数据资源文件
            //泛型是继承自ScriptableObject的类
            var assetData = ScriptableObject.CreateInstance<ScriptableAssetBundle>();
            GetBundleAssetData(manifestList, assetData);
            //前一步创建的资源只是存在内存中，现在要把它保存到本地
            //通过编辑器API，创建一个数据资源文件，第二个参数为资源文件在Assets目录下的路径
            var folder = Path.Combine("Assets", "Resources", Consts.BundleAssetConfigFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var path = Path.Combine("Assets","Resources", Consts.BundleAssetConfigFolder, Consts.BundleAssetConfigName);
            AssetDatabase.CreateAsset(assetData, path);
            //保存创建的资源
            AssetDatabase.SaveAssets();
            //刷新界面
            AssetDatabase.Refresh();
        }

        private static void GetBundleAssetData(List<string> manifestList, ScriptableAssetBundle assetData)
        {
            AssetBundle.UnloadAllAssetBundles(true);
            foreach (var item in manifestList)
            {
                var bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, item));
                if (string.IsNullOrEmpty(bundle.name))
                {
                    bundle.Unload(true);
                    continue;
                }

                var assets = bundle.GetAllAssetNames();
                foreach (var name in assets)
                {
                    assetData.source.Add(new ScriptableAssetBundleData()
                    {
                        hashCode = name.GenHashCode(),
                        assetName = name,
                        assetBundle = bundle.name
                    });
                }

                bundle.Unload(true);
            }

            AssetBundle.UnloadAllAssetBundles(true);
        }

        public static void BuildWindow()
        {
        }

        public static void BuildAndriod()
        {
        }

        public static void BuildSwitch()
        {
        }
    }
}
#endif