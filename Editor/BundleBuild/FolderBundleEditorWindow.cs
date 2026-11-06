using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Serialization;

namespace PowerCellStudio.Editor
{
    public class FolderBundleEditorWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<FolderBundleEditorWindow>("Folder AssetBundle Settings");
        }

        private FolderTreeView treeView;
        private TreeViewState treeViewState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

        [Serializable]
        private class BundleWhitelistCache
        {
            public List<string> bundles = new List<string>();
        }

        private const string WhitelistCachePath = "Assets/UFlowSetting/BundleWhitelist.json";
        private BundleWhitelistCache whitelist = new BundleWhitelistCache();
        private string[] availableBundles = Array.Empty<string>();
        private Vector2 whitelistScrollPosition;
        private bool whitelistLoaded;

        private void LoadWhitelist()
        {
            try
            {
                whitelist = File.Exists(WhitelistCachePath)
                    ? JsonUtility.FromJson<BundleWhitelistCache>(File.ReadAllText(WhitelistCachePath))
                    : new BundleWhitelistCache();
                if (whitelist == null || whitelist.bundles == null)
                    throw new InvalidDataException("Invalid bundle whitelist cache.");
                whitelistLoaded = true;
            }
            catch (Exception exception)
            {
                whitelist = new BundleWhitelistCache();
                whitelistLoaded = false;
                Debug.LogError($"Failed to load bundle whitelist: {exception.Message}");
            }
        }

        private void SaveWhitelist()
        {
            if (!whitelistLoaded) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(WhitelistCachePath));
                var temporaryPath = WhitelistCachePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(whitelist, true));
                if (File.Exists(WhitelistCachePath))
                    File.Replace(temporaryPath, WhitelistCachePath, null);
                else
                    File.Move(temporaryPath, WhitelistCachePath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save bundle whitelist: {exception.Message}");
            }
        }

        private void OnDisable()
        {
            SaveWhitelist();
        }

        private void OnProjectChange()
        {
            RefreshAvailableBundles();
            Repaint();
        }

        private void RefreshAvailableBundles()
        {
            availableBundles = AssetDatabase.GetAllAssetBundleNames();
            Array.Sort(availableBundles, StringComparer.Ordinal);
        }

        private void DrawWhitelist()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Bundle Whitelist", EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(!whitelistLoaded))
                    {
                        if (GUILayout.Button("+", GUILayout.Width(25f)))
                        {
                            RefreshAvailableBundles();
                            whitelist.bundles.Add(string.Empty);
                            SaveWhitelist();
                        }
                    }
                }
                if (!whitelistLoaded)
                {
                    EditorGUILayout.HelpBox("Whitelist cache could not be loaded. Apply is disabled to protect existing bundles.", MessageType.Error);
                    return;
                }
                EditorGUILayout.LabelField("Listed bundles are excluded from cleanup. Folder edits are applied afterward.", EditorStyles.wordWrappedMiniLabel);
                using (var scroll = new EditorGUILayout.ScrollViewScope(whitelistScrollPosition,
                    GUILayout.Height(Mathf.Min(whitelist.bundles.Count, 4) * 24f + 5f)))
                {
                    whitelistScrollPosition = scroll.scrollPosition;
                    for (var i = 0; i < whitelist.bundles.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var current = whitelist.bundles[i];
                            var label = string.IsNullOrEmpty(current) ? "Select Bundle..." : current;
                            if (GUILayout.Button(label, EditorStyles.popup))
                            {
                                RefreshAvailableBundles();
                                var index = i;
                                var menu = new GenericMenu();
                                foreach (var bundle in availableBundles)
                                {
                                    var selectedBundle = bundle;
                                    if (bundle != current && whitelist.bundles.Contains(bundle)) continue;
                                    menu.AddItem(new GUIContent(bundle), bundle == current, () =>
                                    {
                                        if (index >= whitelist.bundles.Count) return;
                                        whitelist.bundles[index] = selectedBundle;
                                        SaveWhitelist();
                                        Repaint();
                                    });
                                }
                                if (menu.GetItemCount() == 0)
                                    menu.AddDisabledItem(new GUIContent("No available bundles"));
                                menu.ShowAsContext();
                            }
                            if (GUILayout.Button("-", GUILayout.Width(25f)))
                            {
                                whitelist.bundles.RemoveAt(i);
                                SaveWhitelist();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            LoadWhitelist();
            RefreshAvailableBundles();
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            bool firstInit = m_MultiColumnHeaderState == null;
            var headerState = CreateHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
            m_MultiColumnHeaderState = headerState;

            var multiColumnHeader = new MultiColumnHeader(headerState);
            if (firstInit)
                multiColumnHeader.ResizeToFit();
            treeView = new FolderTreeView(treeViewState, multiColumnHeader);
        }

        private MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Folder"),
                    width = 200,
                    minWidth = 100,
                    autoResize = true,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Has Bundle"),
                    width = 60,
                    minWidth = 60,
                    autoResize = true,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Bundle Name"),
                    width = 100,
                    minWidth = 100,
                    autoResize = true,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Inherited Bundle"),
                    width = 100,
                    minWidth = 100,
                    autoResize = true,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left
                }
            };
            return new MultiColumnHeaderState(columns);
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, position.size));
            using (new EditorGUILayout.VerticalScope(EditorUIStyle.WindowBackground))
            {
                EditorUIStyle.DrawImguiHeader("AssetBundle Folder Settings", "Assign bundle names to folders and review inherited settings.");
                if (treeView == null)
                {
                    EditorGUILayout.HelpBox("Bundle settings are not ready.", MessageType.Warning);
                    return;
                }

                DrawWhitelist();
                using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition, EditorUIStyle.SectionBox))
                {
                    scrollPosition = scroll.scrollPosition;
                    var rect = GUILayoutUtility.GetRect(0, EditorUIStyle.TreeViewMaxLayoutSize, 0,
                        Mathf.Max(120f, position.height - 215f - Mathf.Min(whitelist.bundles.Count, 4) * 24f));
                    treeView.OnGUI(rect);
                }

                GUILayout.Space(EditorUIStyle.ContentSpacing);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Clear Settings", EditorUIStyle.DestructiveButton))
                    {
                        ConfirmEditorWindow.ShowWindow(() =>
                            {
                                treeView.ClearBundles();
                                AssetDatabase.RemoveUnusedAssetBundleNames();
                                AssetDatabase.SaveAssets();
                                treeView.Reload();
                            },
                            null,
                            "Clear All Bundle Settings",
                            "Are you sure you want to clear all bundle settings?\nThis action cannot be undone.");
                    }

                    GUILayout.Space(EditorUIStyle.ContentSpacing);
                    using (new EditorGUI.DisabledScope(!whitelistLoaded))
                    {
                        if (GUILayout.Button("Apply Settings", EditorUIStyle.PrimaryButton))
                        {
                            treeView.ApplyBundles(new HashSet<string>(whitelist.bundles, StringComparer.Ordinal));
                            AssetDatabase.RemoveUnusedAssetBundleNames();
                            AssetDatabase.SaveAssets();
                            treeView.Reload();
                            RefreshAvailableBundles();
                        }
                    }
                }
            }
        }

        class FolderTreeView : TreeView
        {
            [Serializable]
            class FolderTreeItem : TreeViewItem
            {
                public string path;
                public string bundle;

                public string inheritedBundle;

                public int hasBundle;
                // public bool isEditing;
            }

            private float kRowHeights = 20f;
            private float kToggleWidth = 20f;
            private string[] _allBundleName;

            Dictionary<int, string> editBuffer = new Dictionary<int, string>();

            // public FolderTreeView(TreeViewState state) : base(state)
            // {
            //     Reload();
            // }

            public FolderTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader) : base(state,
                multicolumnHeader)
            {
                // 自定义设置
                rowHeight = 20;
                // columnIndexForTreeFoldouts = 2;
                showAlternatingRowBackgrounds = true;
                showBorder = true;
                customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f;
                // extraSpaceBeforeIconAndLabel = kToggleWidth;
                // multicolumnHeader.sortingChanged += OnSortingChanged;
                _allBundleName = AssetDatabase.GetAllAssetBundleNames();
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                editBuffer.Clear();
                var root = new FolderTreeItem
                    { id = 0, depth = -1, displayName = "Root", path = "Assets", bundle = "", inheritedBundle = "" };
                AddChildren(root, "Assets", null);
                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            void AddChildren(FolderTreeItem parent, string path, string parentBundle)
            {
                var dirs = Directory.GetDirectories(path);
                parent.children = new List<TreeViewItem>();
                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    var setBundle = GetFolderBundle(dir);
                    var hasBundle = !string.IsNullOrEmpty(setBundle);
                    var child = new FolderTreeItem
                    {
                        id = dir.GetHashCode(),
                        displayName = name,
                        path = dir,
                        bundle = setBundle,
                        inheritedBundle = hasBundle
                            ? setBundle
                            : parentBundle,
                        hasBundle = hasBundle ? 1 : 0
                        // isEditing = false
                    };
                    if (hasBundle) 
                    {
                        editBuffer[child.id] = child.bundle;
                    }
                    parent.AddChild(child);
                    AddChildren(child, dir, child.inheritedBundle);
                    if (hasBundle) AddParentBundleCount(parent, 1);
                }
            }

            private void AddParentBundleCount(FolderTreeItem item, int addNumber)
            {
                item.hasBundle += addNumber;
                var parent = item.parent as FolderTreeItem;
                if (parent != null)
                {
                    AddParentBundleCount(parent, addNumber);
                }
            }

            string GetFolderBundle(string folderPath)
            {
                var importer = AssetImporter.GetAtPath(folderPath);
                return importer != null ? importer.assetBundleName : "";
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = (FolderTreeItem)args.item;

                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
                }
            }

            void CellGUI(Rect cellRect, FolderTreeItem item, int column, ref RowGUIArgs args)
            {
                // 使用 EditorGUIUtility.singleLineHeight 垂直居中单元格。
                // 这样可以更轻松地在单元格中放置控件和图标。
                CenterRectUsingSingleLineHeight(ref cellRect);
                switch (column)
                {
                    case 0:
                    {
                        // 在标签文本的左侧创建一个开关按钮
                        Rect toggleRect = cellRect;
                        toggleRect.x += GetContentIndent(item);
                        toggleRect.width = kToggleWidth;
                        // if (toggleRect.xMax < cellRect.xMax)
                        //     item.data.enabled = EditorGUI.Toggle(toggleRect, item.data.enabled);
                        // 默认图标和标签
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                        break;
                    }
                    case 1:
                    {
                        // Bundle是否设置
                        EditorGUI.LabelField(cellRect, item.hasBundle > 0 ? "*" : string.Empty);
                        break;
                    }
                    case 2:
                    {
                        // var style = new GUIStyle(EditorStyles.textField);
                        // Bundle名编辑
                        // string displayBundle = item.bundle;
                        string editValue = item.bundle;
                        // 下拉按钮可选择_allBundleName中的值
                        editValue = EditorGUI.TextField(cellRect, editValue);
                        if (editValue != item.bundle)
                        {
                            var parent = item.parent as FolderTreeItem;
                            if (parent != null && !string.IsNullOrEmpty(parent.inheritedBundle) && parent.inheritedBundle == editValue) 
                            {
                                editValue = string.Empty;
                            }
                            if (string.IsNullOrEmpty(editValue))
                            {
                                item.inheritedBundle = parent?.inheritedBundle ?? "";
                                AddParentBundleCount(item, -1);
                            }
                            else
                            {
                                item.inheritedBundle = editValue;
                                AddParentBundleCount(item, 1);
                            }

                            item.bundle = editValue;
                            ChangeInheritedBundleOfChildren(item, item.inheritedBundle);
                            // editBuffer[item.id] = editValue;
                        }

                        break;
                    }
                    case 3:
                    {
                        // var style = new GUIStyle(EditorStyles.textField);
                        // Bundle名编辑
                        EditorGUI.LabelField(cellRect, item.inheritedBundle);
                        break;
                    }
                    default:
                        break;
                }
            }

            private void ChangeInheritedBundleOfChildren(FolderTreeItem item, string newBundle)
            {
                if (item.hasChildren)
                {
                    foreach (FolderTreeItem child in item.children)
                    {
                        if (!string.IsNullOrEmpty(child.bundle)) continue;
                        child.inheritedBundle = newBundle;
                        ChangeInheritedBundleOfChildren(child, newBundle);
                    }
                }
            }

            // public override void OnGUI(Rect rect)
            // {
            //     base.OnGUI(rect);
            // }

            public void ApplyBundles(HashSet<string> bundleWhitelist)
            {
                foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
                {
                    if (bundleWhitelist.Contains(bundleName)) continue;
                    if (!AssetDatabase.RemoveAssetBundleName(bundleName, true))
                        throw new InvalidOperationException($"Failed to remove asset bundle: {bundleName}");
                }

                ApplyBundlesRecursive(rootItem as FolderTreeItem);
                DealWithDuplicateAssets();
                editBuffer.Clear();
            }

            private void DealWithDuplicateAssets()
            {
                string[] assetBuneleNames = AssetDatabase.GetAllAssetBundleNames();
                var duplicateAssets = new Dictionary<string, HashSet<string>>();
                var assignedAssetPaths = new HashSet<string>();
                var assetPathsByBundle = new string[assetBuneleNames.Length][];
                for (var i = 0; i < assetBuneleNames.Length; i++)
                {
                    var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBuneleNames[i]);
                    assetPathsByBundle[i] = assetPaths;
                    assignedAssetPaths.UnionWith(assetPaths);
                }
                
                for (var i = 0; i < assetPathsByBundle.Length; i++)
                {
                    var assetPaths = assetPathsByBundle[i];
                    string[] dependencies = AssetDatabase.GetDependencies(assetPaths, true);
                    foreach (string dependency in dependencies)
                    {
                        if (!dependency.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                        var extension = Path.GetExtension(dependency);
                    
                        if (extension == ".cs" || extension == ".dll") continue;
                        if (assignedAssetPaths.Contains(dependency)) continue;
                        if (!duplicateAssets.TryGetValue(dependency, out var dependentBundles))
                        {
                            dependentBundles = new HashSet<string>(StringComparer.Ordinal);
                            duplicateAssets.Add(dependency, dependentBundles);
                        }
                        dependentBundles.Add(assetBuneleNames[i]);
                    }
                }

                // 完成全部依赖统计后再修改分包，避免影响本次扫描结果。
                var sortedBundleNames = new List<string>();
                foreach (var asset in duplicateAssets)
                {
                    if (asset.Value.Count < 2) continue;

                    sortedBundleNames.Clear();
                    sortedBundleNames.AddRange(asset.Value);
                    sortedBundleNames.Sort(StringComparer.Ordinal);
                    var bundleName = string.Concat(sortedBundleNames);
                    var importer = AssetImporter.GetAtPath(asset.Key);
                    if (importer == null)
                    {
                        AssetLogger.LogError($"Cannot assign shared asset bundle: {asset.Key}");
                        continue;
                    }

                    importer.SetAssetBundleNameAndVariant(bundleName, string.Empty);
                    EditorUtility.SetDirty(importer);
                }
            }

            void ApplyBundlesRecursive(FolderTreeItem item)
            {
                if (item.hasChildren)
                {
                    foreach (FolderTreeItem child in item.children)
                    {
                        ApplyBundlesRecursive(child);
                    }
                }

                // 清除旧分包后，未编辑的非空配置也必须重新应用。
                var needApply = !string.IsNullOrEmpty(item.bundle);
                if (editBuffer.TryGetValue(item.id, out var value) && !value.Equals(item.bundle))
                {
                    needApply = true;
                }

                if (!needApply) return;

                var importer = AssetImporter.GetAtPath(item.path);
                if (importer != null)
                {
                    importer.assetBundleName = item.bundle;
                    EditorUtility.SetDirty(importer);
                }
            }

            public void ClearBundles()
            {
                ClearBundlesRecursive(rootItem as FolderTreeItem);
                editBuffer.Clear();
            }

            void ClearBundlesRecursive(FolderTreeItem item)
            {
                var importer = AssetImporter.GetAtPath(item.path);
                if (importer != null)
                {
                    importer.assetBundleName = "";
                    EditorUtility.SetDirty(importer);
                }

                if (item.hasChildren)
                {
                    foreach (FolderTreeItem child in item.children)
                    {
                        ClearBundlesRecursive(child);
                    }
                }
            }
        }
    }
}