using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class GenerateRemoteManifestWindow : EditorWindow
    {
        [MenuItem("Build/AssetBundle/Bundle Manifest配置")]
        public static void ShowWindow()
        {
            var window = GetWindow<GenerateRemoteManifestWindow>();
            window.titleContent = new GUIContent("Bundle Manifest配置");
            window.Show();
        }

        public static void ShowWindowWithHandle(Action onCompleted)
        {
            var window = GetWindow<GenerateRemoteManifestWindow>();
            window.titleContent = new GUIContent("Bundle Manifest配置");
            window.onCompleted = onCompleted;
            window.Show();
        }

        private RemoteManifest _remoteManifest;
        public Action onCompleted;
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            string savePath = Path.Combine(Application.streamingAssetsPath, "remoteManifest.json");
            RemoteManifest exitRemoteManifest = null;
            if (File.Exists(savePath))
            {
                var readJson = File.ReadAllText(savePath);
                exitRemoteManifest = JsonConvert.DeserializeObject<RemoteManifest>(readJson);
            }

            // 资源包目录（请根据实际路径修改）
            var buildPath = Path.Combine(Application.streamingAssetsPath,
                AssetsBundleBuildUtils.GetBuildFoldName(EditorUserBuildSettings.activeBuildTarget));
            string[] ignoredFiles = { ".manifest", ".meta" }; // 要忽略的文件
            string[] files = Directory.GetFiles(buildPath)
                .Where(file => !ignoredFiles.Contains(Path.GetExtension(file)))
                .ToArray();
            _remoteManifest = new RemoteManifest();
            foreach (var file in files)
            {
                if (file.EndsWith(".manifest")) continue; // 跳过Unity生成的manifest文件

                BundleInfo info = new BundleInfo();
                info.name = Path.GetFileName(file);
                info.size = new FileInfo(file).Length;
                info.md5 = GetFileMD5(file);
                if (exitRemoteManifest != null)
                    info.isRemote = exitRemoteManifest.bundles.Exists(o => o.name == info.name && o.isRemote);
                else
                    info.isRemote = false;
                _remoteManifest.bundles.Add(info);
            }
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

        private void OnDisable()
        {
            _remoteManifest = null;
            onCompleted = null;
        }

        private void OnGUI()
        {
            if (_remoteManifest == null) return;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUILayout.BeginVertical();
            var needClose = false;
            // 设定区域的高度，可以容纳三行LabelFields
            float lineHeight = EditorGUIUtility.singleLineHeight; // 每行的高度
            float padding = 4; // 行间距
            float totalHeight = lineHeight * 3 + padding * 2 + 20; 
            
            for (var i = 0; i < _remoteManifest.bundles.Count; i++)
            {
                var bundle = _remoteManifest.bundles[i];
                GUI.Box(new Rect(0, (totalHeight + 6) * i, position.width - 20, totalHeight), GUIContent.none);
                
                // 恢复背景颜色为空，以便绘制文本（重要）
                EditorGUILayout.LabelField("Bundle Name", bundle.name);
                bundle.isRemote = EditorGUILayout.Toggle("IsRemote", bundle.isRemote);
                EditorGUILayout.LabelField("Bundle md5", bundle.md5);
                EditorGUILayout.LabelField("Bundle size", $"{bundle.size / 1024f / 1024f} MB");
                EditorGUILayout.Space();
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("Generate"))
            {
                string savePath = Path.Combine(Application.streamingAssetsPath, "remoteManifest.json");
                string json = JsonConvert.SerializeObject(_remoteManifest, Formatting.Indented);
                File.WriteAllText(savePath, json);
                Debug.Log("Manifest生成完成: " + savePath);
                AssetDatabase.Refresh();
                if (onCompleted != null)
                {
                    onCompleted.Invoke();
                    needClose = true;
                }
            }
            
            GUILayout.EndScrollView();
            if (needClose) Close();
        }
    }
}