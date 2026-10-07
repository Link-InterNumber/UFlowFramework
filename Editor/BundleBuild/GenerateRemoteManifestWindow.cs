using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class GenerateRemoteManifestWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<GenerateRemoteManifestWindow>();
            window.titleContent = new GUIContent("Remote Manifest配置");
            window.Show();
        }

        public static void ShowWindowWithHandle(Action onCompleted)
        {
            var window = GetWindow<GenerateRemoteManifestWindow>();
            window.titleContent = new GUIContent("Remote Manifest配置");
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
            var buildPath = Application.streamingAssetsPath;
            string[] ignoredFiles = { ".manifest", ".meta" }; // 要忽略的文件
            // 递归获取目录下的所有文件，并过滤掉不需要的文件
            string[] files = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories)
                .Where(file => !ignoredFiles.Contains(Path.GetExtension(file)))
                .ToArray();
            _remoteManifest = new RemoteManifest();
            foreach (var file in files)
            {
                // 跳过Unity生成的remoteManifest.json文件
                if (Path.GetFileName(file) == "remoteManifest.json") continue;

                BundleInfo info = new BundleInfo();
                // info.name 保存相对路径，方便后续加载时使用
                info.name = file.Substring(buildPath.Length + 1).Replace("\\", "/");
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
            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, position.size));
            EditorUIStyle.DrawImguiHeader("Remote Manifest", "Choose which StreamingAssets files should be delivered remotely.");
            EditorGUILayout.HelpBox($"资源目录 / Asset directory\n{Application.streamingAssetsPath}", MessageType.Info);
            if (_remoteManifest == null)
            {
                EditorGUILayout.HelpBox("No manifest data is available.", MessageType.Warning);
                return;
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition);
            _scrollPosition = scroll.scrollPosition;
            var needClose = false;

            for (var i = 0; i < _remoteManifest.bundles.Count; i++)
            {
                var bundle = _remoteManifest.bundles[i];
                using (new EditorGUILayout.VerticalScope(EditorUIStyle.ManifestCard))
                {
                    EditorGUILayout.LabelField(bundle.name, EditorUIStyle.SectionTitle);
                    bundle.isRemote = EditorGUILayout.Toggle("Remote delivery", bundle.isRemote);
                    EditorGUILayout.SelectableLabel(bundle.md5, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.LabelField("Size", $"{bundle.size / 1024f / 1024f:F2} MB");
                }
                GUILayout.Space(EditorUIStyle.ManifestCardSpacing);
            }

            if (GUILayout.Button("Generate Manifest", EditorUIStyle.PrimaryButton))
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
            
            if (needClose) Close();
        }
    }
}