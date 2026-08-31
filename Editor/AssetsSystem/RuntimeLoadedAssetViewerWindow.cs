using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PowerCellStudio;

namespace PowerCellStudio.Editor
{
    public sealed class RuntimeLoadedAssetViewerWindow : EditorWindow
    {
        private readonly List<LoaderGroup> _groups = new List<LoaderGroup>();
        private readonly Dictionary<string, int> _assetUsageCount = new Dictionary<string, int>();
        private Vector2 _scroll;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;

        public static void ShowWindow()
        {
            var window = GetWindow<RuntimeLoadedAssetViewerWindow>();
            window.titleContent = new GUIContent("Loaded Assets");
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Loaded Assets");
            RefreshData();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RefreshData();
            Repaint();
        }

        private void OnProjectChanged()
        {
            if (Application.isPlaying)
            {
                RefreshData();
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup - _lastRefreshTime < 0.5d)
            {
                return;
            }

            RefreshData();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect active asset loaders.", MessageType.Info);
                return;
            }

            if (_groups.Count == 0)
            {
                EditorGUILayout.HelpBox("No active loaders or no loaded assets were found.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var group in _groups)
            {
                DrawTagGroup(group);
                GUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();

            DrawSummary();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    RefreshData();
                    Repaint();
                }

                _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(90));

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Loaders: {_groups.Count}", EditorStyles.miniLabel, GUILayout.Width(90));
                EditorGUILayout.LabelField($"Assets: {_assetUsageCount.Count}", EditorStyles.miniLabel, GUILayout.Width(90));
            }
        }

        private void DrawTagGroup(LoaderGroup group)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(string.IsNullOrEmpty(group.Tag) ? "<No Tag>" : group.Tag, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Loaders: {group.Loaders.Count}    Unique Assets: {group.UniqueAssets.Count}", EditorStyles.miniLabel);

                foreach (var loader in group.Loaders)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
                    {
                        EditorGUILayout.LabelField($"Loader #{loader.Index}   Index: {loader.Index}", EditorStyles.miniBoldLabel);
                        if (loader.Assets.Count == 0)
                        {
                            EditorGUILayout.LabelField("No loaded assets.", EditorStyles.miniLabel);
                            continue;
                        }

                        for (int i = 0; i < loader.Assets.Count; i++)
                        {
                            var path = loader.Assets[i];
                            DrawAssetRow(path);
                        }
                    }
                }
            }
        }

        private void DrawAssetRow(string path)
        {
            _assetUsageCount.TryGetValue(path, out var count);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"[{count}]", GUILayout.Width(36));

                if (GUILayout.Button(path, EditorStyles.linkLabel))
                {
                    PingAsset(path);
                }
            }
        }

        private void DrawSummary()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Cross-Loader Usage Count", EditorStyles.boldLabel);
                if (_assetUsageCount.Count == 0)
                {
                    EditorGUILayout.LabelField("No assets.", EditorStyles.miniLabel);
                    return;
                }

                foreach (var pair in _assetUsageCount)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"[{pair.Value}]", GUILayout.Width(36));
                        if (GUILayout.Button(pair.Key, EditorStyles.linkLabel))
                        {
                            PingAsset(pair.Key);
                        }
                    }
                }
            }
        }

        private void RefreshData()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            _groups.Clear();
            _assetUsageCount.Clear();

            var groupLookup = new Dictionary<string, LoaderGroup>(StringComparer.Ordinal);
            foreach (var loader in AssetUtils.GetAllActiveLoaders())
            {
                if (loader == null || !loader.spawned)
                {
                    continue;
                }

                if (!groupLookup.TryGetValue(loader.tag ?? string.Empty, out var group))
                {
                    group = new LoaderGroup(loader.tag ?? string.Empty);
                    groupLookup.Add(group.Tag, group);
                    _groups.Add(group);
                }

                var loaderInfo = new LoaderInfo(loader.index);
                group.Loaders.Add(loaderInfo);

                var assetEnumerator = loader.GetAllLoadedAssets();
                foreach (var path in assetEnumerator)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    loaderInfo.Assets.Add(path);
                    if (group.UniqueAssets.Add(path))
                    {
                        // unique within tag only
                    }

                    if (_assetUsageCount.TryGetValue(path, out var count))
                    {
                        _assetUsageCount[path] = count + 1;
                    }
                    else
                    {
                        _assetUsageCount[path] = 1;
                    }
                }
            }

            _groups.Sort((a, b) => string.CompareOrdinal(a.Tag, b.Tag));
            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].Loaders.Sort((a, b) => a.Index.CompareTo(b.Index));
            }
        }

        private static void PingAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                Debug.LogWarning($"Asset not found in Project window: {path}");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private sealed class LoaderGroup
        {
            public string Tag { get; }
            public List<LoaderInfo> Loaders { get; } = new List<LoaderInfo>();
            public HashSet<string> UniqueAssets { get; } = new HashSet<string>(StringComparer.Ordinal);

            public LoaderGroup(string tag)
            {
                Tag = tag ?? string.Empty;
            }
        }

        private sealed class LoaderInfo
        {
            public int Index { get; }
            public List<string> Assets { get; } = new List<string>();

            public LoaderInfo(int index)
            {
                Index = index;
            }
        }
    }
}