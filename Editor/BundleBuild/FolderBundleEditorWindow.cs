using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Serialization;

namespace PowerCellStudio
{
    public class FolderBundleEditorWindow : EditorWindow
    {
        [MenuItem("Build/AssetBundle/Folder AssetBundle Settings")]
        public static void ShowWindow()
        {
            GetWindow<FolderBundleEditorWindow>("Folder AssetBundle Settings");
        }

        private FolderTreeView treeView;
        private TreeViewState treeViewState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;

        private void OnEnable()
        {
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
            EditorGUILayout.LabelField("Set Folder's Bundle", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, position.height - 60);
            treeView.OnGUI(rect);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            // 红色的Clear Setting按钮
            GUI.backgroundColor = Color.red;
            if ( GUILayout.Button("Clear Setting"))
            {
                // 弹出对话框确认
                ConfirmEditorWindow.ShowWindow(() =>
                    {
                        treeView.ClearBundles();
                        AssetDatabase.RemoveUnusedAssetBundleNames();
                        AssetDatabase.SaveAssets();
                        treeView.Reload();
                    },
                    null,
                    "Clear All Bundle Settings",
                    "Are you sure you want to clear all bundle settings?\n This action cannot be undone.");
            }
            
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Apply Setting"))
            {
                treeView.ApplyBundles();
                AssetDatabase.RemoveUnusedAssetBundleNames();
                AssetDatabase.SaveAssets();
                treeView.Reload();
            }
            
            EditorGUILayout.EndHorizontal();

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
                            if (parent != null && parent.inheritedBundle == editValue) break;
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

            public void ApplyBundles()
            {
                ApplyBundlesRecursive(rootItem as FolderTreeItem);
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

                var needApply = false;
                if (editBuffer.TryGetValue(item.id, out var value))
                {
                    needApply = !value.Equals(item.bundle);
                }
                else if (!string.IsNullOrEmpty(item.bundle))
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