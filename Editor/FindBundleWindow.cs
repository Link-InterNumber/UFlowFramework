using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public class FindBundleWindow : FolderEditorWindow
    {
        Dictionary<string,List<string>> dataString = new Dictionary<string, List<string>>();
        private string _bundleName = "";
        private string _printResult;
        private Vector2 _scrollPosition;

        protected override void OnGUI()
        {
            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, position.size));
            EditorUIStyle.DrawImguiHeader("Bundle Analyzer", "Estimate dependency size or export bundle dependency information.");
            using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
            {
                EditorGUILayout.LabelField("Load Cost", EditorUIStyle.SectionTitle);
                _bundleName = EditorGUILayout.TextField("Bundle name", _bundleName);
                DrawButton("计算 Bundle 加载消耗", ShowLoadBundleCost, EditorUIStyle.PrimaryButton);
            }

            if (!string.IsNullOrEmpty(_printResult))
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition, EditorUIStyle.SectionBox,
                           GUILayout.MinHeight(100f), GUILayout.MaxHeight(260f)))
                {
                    _scrollPosition = scroll.scrollPosition;
                    GUILayout.Label(_printResult, EditorUIStyle.RichTextLabel);
                }
            }

            GUILayout.Space(EditorUIStyle.ContentSpacing);
            base.OnGUI();
            DrawButton("输出 Bundle 信息", CollectAssetBundleData, EditorUIStyle.PrimaryButton);
        }

        private void ShowLoadBundleCost(string[] guids)
        {
            var cost = ShowLoadBundleCostHandle(_bundleName, out var needLoadBundles);
            var sb = new StringBuilder();
            var mb = cost / 1024f / 1024f;
            sb.Append($"加载bundle:{_bundleName} 需要加载的bundle有{needLoadBundles.Count}个，总大小为\t<color=#ff0000>{mb} mb</color>\n");
            foreach (var needLoadBundle in needLoadBundles)
            {
                sb.Append($"加载bundle:{needLoadBundle.Item1} 需要加载的大小为\t{needLoadBundle.Item2 / 1024f / 1024f} mb\n");
            }
            _printResult = sb.ToString();
            sb.Clear();
            sb = null;
        }
        
        private static long ShowLoadBundleCostHandle(string bundleName, out List<(string, long)> needLoadBundles)
        {
            needLoadBundles = new List<(string, long)>();
            if(bundleName == null)
            {
                return 0;
            }
            var minifestBundle = AssetBundle.LoadFromFile("Assets/StreamingAssets/StandaloneWindows.bundle");
            var minifest = minifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            var bundles = GetDependentBundles(bundleName, minifest);
            var totalSize = 0L;
            foreach (var needLoadBundle in bundles)
            {
                var size = GetBundleSizeOnEditor(needLoadBundle);
                totalSize += size;
                needLoadBundles.Add((needLoadBundle, size));
            }
            needLoadBundles.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            minifestBundle.Unload(true);
            return totalSize;
        }
        
        private static HashSet<string> GetDependentBundles(string bundleName, AssetBundleManifest manifest)
        {
            if (manifest == null)
                return new HashSet<string>();
            HashSet<string> dependentBundles = new HashSet<string>();
            dependentBundles.Add(bundleName);
            string[] dependencies = manifest.GetAllDependencies(bundleName);
            foreach (string dependency in dependencies)
            {
                if (!string.IsNullOrEmpty(dependency) && !dependentBundles.Contains(dependency))
                {
                    dependentBundles.Add(dependency);
                }
            }
            return dependentBundles;
        }
        
        private static long GetBundleSizeOnEditor(string bundleName)
        {
            var bundlePath = Path.Combine(Application.streamingAssetsPath, bundleName);
            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"bundle文件不存在：{bundlePath}");
                return 0;
            }
            FileInfo fileInfo = new FileInfo(bundlePath);
            return fileInfo.Length;
        }

        private List<string> GetAllDependentBundles(string assetPath)
        {
            List<string> dependentBundles = new List<string>();
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
            foreach (string dependency in dependencies)
            {
                string bundleName = AssetDatabase.GetImplicitAssetBundleName(dependency);
                if (!string.IsNullOrEmpty(bundleName) && !dependentBundles.Contains(bundleName))
                {
                    dependentBundles.Add(bundleName);
                }
            }
            return dependentBundles;
        }
        
        private void CollectAssetBundleData(string[] guids)
        {
            dataString.Clear();
            foreach (var uid in guids)
            {
                string directoryPath = AssetDatabase.GUIDToAssetPath(uid);
                if(Directory.Exists(directoryPath)) continue;
                var allBundleName = GetAllDependentBundles(directoryPath);
                dataString.Add(directoryPath, allBundleName);
            }
            
            var text = new StringBuilder();
            text.AppendLine("资源名,bundle名");
            foreach (var asset in dataString)
            {
                var bundles = new StringBuilder();
                asset.Value.Sort();
                foreach (var bundle in asset.Value)
                {
                    bundles.Append($"{bundle},");
                }
                text.AppendLine($"{asset.Key},{bundles}");
            }

            WriteDataToFile("Assets/Debug", "AssetBundleData.csv", text.ToString());
            dataString.Clear();
            text.Clear();
        }

        public static void SetTextureFormat()
        {
            EditorWindow.GetWindow<FindBundleWindow>(false, "查找资源bundle", true).Show();
        }
        
        protected override string _filter => "";
    }
}