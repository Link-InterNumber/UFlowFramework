using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace PowerCellStudio
{
    public class GenerateRemoteManifest
    {
        [MenuItem("Tools/生成本地Manifest")]
        public static void GenerateLocalManifest()
        {
            // 资源包目录（请根据实际路径修改）
            string[] files = Directory.GetFiles(Application.streamingAssetsPath, "*.bundle", SearchOption.TopDirectoryOnly);

            RemoteManifest manifest = new RemoteManifest();

            foreach (var file in files)
            {
                if (file.EndsWith(".manifest")) continue; // 跳过Unity生成的manifest文件

                BundleInfo info = new BundleInfo();
                info.name = Path.GetFileName(file);
                info.size = new FileInfo(file).Length;
                info.md5 = GetFileMD5(file);

                manifest.bundles.Add(info);
            }

            string json = JsonConvert.SerializeObject(manifest);
            string savePath = Path.Combine(Application.streamingAssetsPath, "remoteManifest.json");
            File.WriteAllText(savePath, json);

            Debug.Log("Manifest生成完成: " + savePath);
            AssetDatabase.Refresh();
        }

        static string GetFileMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
