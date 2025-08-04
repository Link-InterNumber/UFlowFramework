using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Serialization;
using UnityEditor.AddressableAssets;

namespace PowerCellStudio
{
    public class FolderAddressableGroupEditorWindow : EditorWindow
    {
        [MenuItem("Build/Addressable/Folder Addressable Settings", false, 800)]
        public static void ShowWindow()
        {
            GetWindow<FolderAddressableGroupEditorWindow>("Folder Addressable Settings");
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
                    headerContent = new GUIContent("Has Group"),
                    width = 60,
                    minWidth = 60,
                    autoResize = true,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Group Name"),
                    width = 100,
                    minWidth = 100,
                    autoResize = true,
                    canSort = false,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Inherited Group"),
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
            EditorGUILayout.LabelField("Set Folder's Addressable Group", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, position.height - 60);
            treeView.OnGUI(rect);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            // 红色的Clear Setting按钮
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Setting"))
            {
                // 弹出对话框确认
                treeView.ClearGroup();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                treeView.Reload();
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Apply Setting"))
            {
                treeView.ApplyGroup();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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
                public string group;

                public string inheritedGroup;

                public int hasGroup;
                // public bool isEditing;
            }

            private float kRowHeights = 20f;
            private float kToggleWidth = 20f;
            private string[] _allGroupName;
            private Dictionary<string, int> _groupNameToIndex = new Dictionary<string, int>();

            private UnityEditor.AddressableAssets.Settings.AddressableAssetSettings _settings;

            Dictionary<int, string> _editBuffer = new Dictionary<int, string>();

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
                _settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                if (_settings != null)
                {
                    var tempList = new List<string>();
                    tempList.Add(string.Empty);
                    for (int i = 0; i < _settings.groups.Count; i++)
                    {
                        UnityEditor.AddressableAssets.Settings.AddressableAssetGroup group = _settings.groups[i];
                        if (group != null && !string.IsNullOrEmpty(group.Name) && !group.Name.Equals("Built In Data"))
                            tempList.Add(group.Name);
                    }
                    _allGroupName = tempList.ToArray();
                    Array.Sort(_allGroupName);
                    _groupNameToIndex.Clear();
                    // _groupNameToIndex[string.Empty] = -1;
                    for (int i = 0; i < _allGroupName.Length; i++)
                    {
                        _groupNameToIndex[_allGroupName[i]] = i;
                    }
                }
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new FolderTreeItem
                { id = 0, depth = -1, displayName = "Root", path = "Assets", group = "", inheritedGroup = "" };
                AddChildren(root, "Assets", null);
                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            void AddChildren(FolderTreeItem parent, string path, string parentGroup)
            {
                var dirs = Directory.GetDirectories(path);
                parent.children = new List<TreeViewItem>();
                foreach (var dir in dirs)
                {
                    var adaptPath = AssetUtils.EditorCheckPath(dir);
                    var guid = AssetDatabase.AssetPathToGUID(dir);
                    var name = Path.GetFileName(dir);
                    var setGroup = GetFolderGroup(guid);
                    var hasGroup = !string.IsNullOrEmpty(setGroup);
                    var child = new FolderTreeItem
                    {
                        id = adaptPath.GetHashCode(),
                        displayName = name,
                        path = adaptPath,
                        group = setGroup,
                        inheritedGroup = hasGroup
                            ? setGroup
                            : parentGroup,
                        hasGroup = hasGroup ? 1 : 0
                        // isEditing = false
                    };
                    if (hasGroup)
                    {
                        _editBuffer[child.id] = child.group;
                    }
                    parent.AddChild(child);
                    AddChildren(child, dir, child.inheritedGroup);
                    if (hasGroup) AddParentGroupCount(parent, 1);
                }
            }

            private void AddParentGroupCount(FolderTreeItem item, int addNumber)
            {
                item.hasGroup += addNumber;
                var parent = item.parent as FolderTreeItem;
                if (parent != null)
                {
                    AddParentGroupCount(parent, addNumber);
                }
            }

            string GetFolderGroup(string guid)
            {
                if (_settings == null) return string.Empty;
                var entry = _settings.FindAssetEntry(guid);
                if (entry == null)  return string.Empty;
                return entry.parentGroup.name;
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
                            EditorGUI.LabelField(cellRect, item.hasGroup > 0 ? "*" : string.Empty);
                            break;
                        }
                    case 2:
                        {
                            int selected = _groupNameToIndex.TryGetValue(item.group, out var index) ? index : 0;
                            int newSelected = EditorGUI.Popup(cellRect, selected, _allGroupName);
                            var editValue = newSelected > -1 ? _allGroupName[newSelected] : string.Empty;
                            if (editValue != item.group)
                            {
                                var parent = item.parent as FolderTreeItem;
                                if (parent != null && parent.inheritedGroup == editValue) break;
                                if (string.IsNullOrEmpty(editValue))
                                {
                                    item.inheritedGroup = parent?.inheritedGroup ?? "";
                                    AddParentGroupCount(item, -1);
                                }
                                else
                                {
                                    item.inheritedGroup = editValue;
                                    AddParentGroupCount(item, 1);
                                }

                                item.group = editValue;
                                ChangeInheritedGroupOfChildren(item, item.inheritedGroup);
                                // editBuffer[item.id] = editValue;
                            }

                            break;
                        }
                    case 3:
                        {
                            // var style = new GUIStyle(EditorStyles.textField);
                            EditorGUI.LabelField(cellRect, item.inheritedGroup);
                            break;
                        }
                    default:
                        break;
                }
            }

            private void ChangeInheritedGroupOfChildren(FolderTreeItem item, string newGroup)
            {
                if (item.hasChildren)
                {
                    foreach (FolderTreeItem child in item.children)
                    {
                        if (!string.IsNullOrEmpty(child.group)) continue;
                        child.inheritedGroup = newGroup;
                        ChangeInheritedGroupOfChildren(child, newGroup);
                    }
                }
            }

            // public override void OnGUI(Rect rect)
            // {
            //     base.OnGUI(rect);
            // }

            public void ApplyGroup()
            {
                ApplyGroupRecursive(rootItem as FolderTreeItem);
            }

            void ApplyGroupRecursive(FolderTreeItem item)
            {
                if (item.hasChildren)
                {
                    foreach (FolderTreeItem child in item.children)
                    {
                        ApplyGroupRecursive(child);
                    }
                }
                var needApply = false;
                if (_editBuffer.TryGetValue(item.id, out var value))
                {
                    needApply = !value.Equals(item.group);
                }
                else if (!string.IsNullOrEmpty(item.group))
                {
                    needApply = true;
                }

                if (!needApply) return;
                var groupName = item.group;
                if (groupName == null || groupName.Equals(string.Empty))
                {
                    var entry = _settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(item.path));
                    if (entry != null)
                    {
                        _settings.RemoveAssetEntry(entry.guid);
                    }
                }
                else
                {
                    var group = _settings.FindGroup(groupName);
                    if (group == null)
                    {
                        return;
                        // group = _settings.CreateGroup(groupName, false, false, false, null, null, null);
                    }
                    var entry = _settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(item.path), group);
                    entry.address = item.path;
                }
            }

            public void ClearGroup()
            {
                if (_settings == null) return;
                ClearBundlesRecursive(rootItem as FolderTreeItem);
                _editBuffer.Clear();
            }
            
            void ClearBundlesRecursive(FolderTreeItem item)
            {
                if (_editBuffer.TryGetValue(item.id, out var groupName))
                {
                    var entry = _settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(item.path));
                    if (entry != null)
                    {
                        _settings.RemoveAssetEntry(entry.guid);
                    }
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