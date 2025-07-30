using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;

public class FolderBundleEditorWindow : EditorWindow
{
    [MenuItem("Tools/Folder AssetBundle 设置(TreeView)")]
    public static void ShowWindow()
    {
        GetWindow<FolderBundleEditorWindow>("文件夹AssetBundle设置(TreeView)");
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
        treeView = new FolderTreeView(treeViewState);
    }

    private MultiColumnHeaderState CreateHeaderState()
    {
        var columns = new[]
        {
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("文件夹"),
                width = 200,
                minWidth = 100,
                autoResize = true,
                canSort = false,
                sortedAscending = true,
                sortingArrowAlignment = TextAlignment.Left
            },
            new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Bundle名"),
                width = 150,
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
        EditorGUILayout.LabelField("文件夹树状结构（可编辑Bundle名）", EditorStyles.boldLabel);
        // scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, position.height - 60);
        treeView.OnGUI(rect);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存"))
        {
            treeView.ApplyBundles();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            treeView.Reload();
        }
        if (GUILayout.Button("清除"))
        {
            treeView.ClearBundles();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
            treeView.Reload();
        }
        EditorGUILayout.EndHorizontal();
        // EditorGUILayout.EndScrollView();
    }

    class FolderTreeView : TreeView
    {
        [Serializable]
        class FolderTreeItem : TreeViewItem
        {
            public string path;
            public string bundle;
            public string inheritedBundle;
            public bool isEditing;
        }

        private float kRowHeights = 20f;
        private float kToggleWidth = 20f;

        Dictionary<int, string> editBuffer = new Dictionary<int, string>();

        public FolderTreeView(TreeViewState state) : base(state)
        {
            Reload();
        }

        public FolderTreeView (TreeViewState state, 
                                    MultiColumnHeader multicolumnHeader) 
                                    : base (state, multicolumnHeader)
        {
            // 自定义设置
            rowHeight = 20;
            // columnIndexForTreeFoldouts = 2;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; 
            // extraSpaceBeforeIconAndLabel = kToggleWidth;
            // multicolumnHeader.sortingChanged += OnSortingChanged;
                    
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new FolderTreeItem { id = 0, depth = -1, displayName = "Root", path = "Assets", bundle = "", inheritedBundle = "" };
            AddChildren(root, "Assets", null);
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        void AddChildren(FolderTreeItem parent, string path, string parentBundle)
        {
            string bundle = GetFolderBundle(path);
            string inheritedBundle = string.IsNullOrEmpty(bundle) ? parentBundle : bundle;

            var dirs = Directory.GetDirectories(path);
            int id = path.GetHashCode();
            parent.children = new List<TreeViewItem>();
            foreach (var dir in dirs)
            {
                var name = Path.GetFileName(dir);
                var child = new FolderTreeItem
                {
                    id = dir.GetHashCode(),
                    displayName = name,
                    path = dir,
                    bundle = GetFolderBundle(dir),
                    inheritedBundle = string.IsNullOrEmpty(GetFolderBundle(dir)) ? inheritedBundle : GetFolderBundle(dir),
                    isEditing = false
                };
                AddChildren(child, dir, child.inheritedBundle);
                parent.AddChild(child);
            }
        }

        string GetFolderBundle(string folderPath)
        {
            var importer = AssetImporter.GetAtPath(folderPath);
            return importer != null ? importer.assetBundleName : "";
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (FolderTreeItem) args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }

            // var item = args.item as FolderTreeItem;
            // Rect r = args.rowRect;
            // float nameWidth = r.width * 0.4f;
            // float bundleWidth = r.width * 0.4f;

            // // 文件夹名
            // EditorGUI.LabelField(new Rect(r.x, r.y, nameWidth, r.height), item.displayName);

            // // Bundle名编辑
            // string displayBundle = string.IsNullOrEmpty(item.bundle) ? item.inheritedBundle : item.bundle;
            // string editValue = editBuffer.ContainsKey(item.id) ? editBuffer[item.id] : displayBundle;
            // editValue = EditorGUI.TextField(new Rect(r.x + nameWidth, r.y, bundleWidth, r.height), editValue);

            // if (editValue != displayBundle)
            //     editBuffer[item.id] = editValue;

            // EditorGUI.LabelField(new Rect(r.x + nameWidth + bundleWidth, r.y, r.width - nameWidth - bundleWidth, r.height), $"- 所属bundle: {displayBundle}");
        }

        void CellGUI (Rect cellRect, FolderTreeItem item, int column, ref RowGUIArgs args)
        {
            // 使用 EditorGUIUtility.singleLineHeight 垂直居中单元格。
            // 这样可以更轻松地在单元格中放置控件和图标。
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case 0:
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
                case 1:
                    var style = new GUIStyle(EditorStyles.textField);
                    // Bundle名编辑
                    string displayBundle = string.IsNullOrEmpty(item.bundle) ? item.inheritedBundle : item.bundle;
                    string editValue = editBuffer.ContainsKey(item.id) ? editBuffer[item.id] : displayBundle;
                    editValue = EditorGUI.TextField(cellRect, editValue);
                    if (editValue != displayBundle)
                        editBuffer[item.id] = editValue;
                    break;
                default:
                    break;
            }
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
        }

        public void ApplyBundles()
        {
            ApplyBundlesRecursive(rootItem as FolderTreeItem, null);
        }

        void ApplyBundlesRecursive(FolderTreeItem item, string parentBundle)
        {
            string newBundle = editBuffer.ContainsKey(item.id) ? editBuffer[item.id] : (string.IsNullOrEmpty(item.bundle) ? parentBundle : item.bundle);

            if (!string.IsNullOrEmpty(newBundle) && newBundle != parentBundle)
            {
                var importer = AssetImporter.GetAtPath(item.path);
                if (importer != null)
                {
                    importer.assetBundleName = newBundle;
                    EditorUtility.SetDirty(importer);
                }
            }
            else if (string.IsNullOrEmpty(newBundle) && !string.IsNullOrEmpty(parentBundle))
            {
                // 不操作，继承父bundle
            }
            else
            {
                var importer = AssetImporter.GetAtPath(item.path);
                if (importer != null)
                {
                    importer.assetBundleName = "";
                    EditorUtility.SetDirty(importer);
                }
            }

            if (item.hasChildren)
            {
                foreach (FolderTreeItem child in item.children)
                {
                    ApplyBundlesRecursive(child, newBundle);
                }
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