#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace PowerCellStudio.Editor
{
    [Serializable]
    public class NotifyTreeElement : TreeViewItem
    {
        public Type notifyEnumType;
        public int notifyIndex;
        public bool isOn;
        public int notifyNumber;
        public int notifyValue;
    }

    public class NotifyTree : TreeView
    {
        private readonly Type _notifyEnumType;
        private float _kRowHeights = 20f;
        private float _kToggleWidth = 20f;

        public NotifyTree(TreeViewState state, Type notifyEnumType) : base(state)
        {
            _notifyEnumType = notifyEnumType;
            if(!Application.isPlaying) return;
            Reload();
        }

        public NotifyTree(TreeViewState state, MultiColumnHeader multiColumnHeader, Type notifyEnumType) : base(state, multiColumnHeader)
        {
            _notifyEnumType = notifyEnumType;
            rowHeight = 20;
            // columnIndexForTreeFoldouts = 2;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (_kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; 
            // extraSpaceBeforeIconAndLabel = _kToggleWidth;
            // multiColumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (NotifyTreeElement) args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }
        
        void CellGUI (Rect cellRect, NotifyTreeElement item, MyColumns column, ref RowGUIArgs args)
        {
            // 使用 EditorGUIUtility.singleLineHeight 垂直居中单元格。
            // 这样可以更轻松地在单元格中放置控件和图标。
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case MyColumns.Name:
                    // 在标签文本的左侧创建一个开关按钮
                    Rect toggleRect = cellRect;
                    toggleRect.x += GetContentIndent(item);
                    toggleRect.width = _kToggleWidth;
                    // if (toggleRect.xMax < cellRect.xMax)
                    //     item.data.enabled = EditorGUI.Toggle(toggleRect, item.data.enabled);
                    // 默认图标和标签
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                    break;
                case MyColumns.IsOn:
                    var style = new GUIStyle(EditorStyles.label);
                    style.normal.textColor =item.isOn ? Color.green : Color.red;
                    EditorGUI.LabelField(cellRect, item.isOn ? "On" : "Off", style);
                    break;
                case MyColumns.Number:
                    EditorGUI.LabelField(cellRect, item.notifyNumber.ToString());
                    break;
                case MyColumns.Value:
                    EditorGUI.LabelField(cellRect, item.notifyValue.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(column), column, null);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var id = 0;
            var root = new NotifyTreeElement()
            {
                id = id,
                depth = -1,
                displayName = _notifyEnumType == null ? "Root" : _notifyEnumType.Name
            };
            id++;
            if (!Application.isPlaying || _notifyEnumType == null) return root;

            var allTypes = Enum.GetValues(_notifyEnumType);
            if (allTypes.Length == 0) return root;

            var rootIndex = FindRootIndex(allTypes);
            if (rootIndex < 0) return root;

            NotifyManager.instance.TryGetNotifyNodeInfo(_notifyEnumType, rootIndex, out root.isOn,
                out root.notifyNumber, out root.notifyValue, out _, out _);

            var childrenNode = new List<NotifyTreeElement>();
            for (var i = 0; i < allTypes.Length; i++)
            {
                AddNodeIfChildOfRoot(allTypes, i, rootIndex, root, childrenNode, ref id);
            }

            while (childrenNode.Count > 0)
            {
                var executeList = new List<NotifyTreeElement>(childrenNode);
                childrenNode.Clear();
                for (var i = 0; i < executeList.Count; i++)
                {
                    var parent = executeList[i];
                    if (!NotifyManager.instance.TryGetNotifyNodeInfo(_notifyEnumType, parent.notifyIndex,
                            out _, out _, out _, out _, out var childIndices))
                        continue;
                    foreach (var childIndex in childIndices)
                    {
                        var node = CreateNode(allTypes, childIndex, id++);
                        parent.AddChild(node);
                        childrenNode.Add(node);
                    }
                }
            }
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private int FindRootIndex(Array enumValues)
        {
            for (var i = 0; i < enumValues.Length; i++)
            {
                if (string.Equals(Enum.GetName(_notifyEnumType, enumValues.GetValue(i)), "Root",
                        StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void AddNodeIfChildOfRoot(Array enumValues, int index, int rootIndex, NotifyTreeElement root,
            List<NotifyTreeElement> childrenNode, ref int id)
        {
            if (!NotifyManager.instance.TryGetNotifyNodeInfo(_notifyEnumType, index,
                    out _, out _, out _, out var parentIndex, out _) || index == rootIndex || parentIndex != rootIndex)
                return;
            var node = CreateNode(enumValues, index, id++);
            root.AddChild(node);
            childrenNode.Add(node);
        }

        private NotifyTreeElement CreateNode(Array enumValues, int index, int id)
        {
            NotifyManager.instance.TryGetNotifyNodeInfo(_notifyEnumType, index, out var isOn, out var number,
                out var value, out _, out _);
            var enumValue = enumValues.GetValue(index);
            return new NotifyTreeElement
            {
                notifyEnumType = _notifyEnumType,
                notifyIndex = index,
                isOn = isOn,
                notifyNumber = number,
                notifyValue = value,
                id = id,
                displayName = Enum.GetName(_notifyEnumType, enumValue) ?? enumValue.ToString()
            };
        }
    }
    
    internal class MyMultiColumnHeader : MultiColumnHeader
    {
        Mode m_Mode;

        public enum Mode
        {
            LargeHeader,
            DefaultHeader,
            MinimumHeaderWithoutSorting
        }

        public MyMultiColumnHeader(MultiColumnHeaderState state)
            : base(state)
        {
            mode = Mode.DefaultHeader;
        }

        public Mode mode
        {
            get
            {
                return m_Mode;
            }
            set
            {
                m_Mode = value;
                switch (m_Mode)
                {
                    case Mode.LargeHeader:
                        canSort = true;
                        height = 37f;
                        break;
                    case Mode.DefaultHeader:
                        canSort = true;
                        height = DefaultGUI.defaultHeight;
                        break;
                    case Mode.MinimumHeaderWithoutSorting:
                        canSort = false;
                        height = DefaultGUI.minimumHeight;
                        break;
                }
            }
        }

        protected override void ColumnHeaderGUI (MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            // Default column header gui
            base.ColumnHeaderGUI(column, headerRect, columnIndex);

            // Add additional info for large header
            if (mode == Mode.LargeHeader)
            {
                // Show example overlay stuff on some of the columns
                if (columnIndex > 2)
                {
                    headerRect.xMax -= 3f;
                    var oldAlignment = EditorStyles.largeLabel.alignment;
                    EditorStyles.largeLabel.alignment = TextAnchor.UpperRight;
                    GUI.Label(headerRect, 36 + columnIndex + "%", EditorStyles.largeLabel);
                    EditorStyles.largeLabel.alignment = oldAlignment;
                }
            }
        }
    }
    
    enum MyColumns
    {
        Name,
        IsOn,
        Number,
        Value,
    }
    
    public class NotifyTreeViewWindow : EditorWindow
    {
        [NonSerialized] bool m_Initialized;
        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
        SearchField m_SearchField;
        NotifyTree m_TreeView;
        [NonSerialized] List<Type> m_NotifyGroupTypes;
        [NonSerialized] Type m_SelectedNotifyGroup;

        public NotifyTree treeView
        {
            get { return m_TreeView; }
        }
        
        Rect multiColumnTreeViewRect
        {
            get { return new Rect(20, 55, position.width - 40, position.height - 85); }
        }

        Rect refreshButtonRect
        {
            get { return new Rect(20, 10, 60, 20); }
        }
        
        Rect toolbarRect
        {
            get { return new Rect (90f, 10f, position.width-110f, 20f); }
        }

        Rect groupToolbarRect
        {
            get { return new Rect(90f, 32f, position.width - 110f, 20f); }
        }
        
        void InitIfNeeded ()
        {
            if (m_Initialized) return;
            RefreshNotifyGroups();
            if (m_SelectedNotifyGroup == null) return;
            // Check if it already exists (deserialized from window layout file or scriptable object)
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            bool firstInit = m_MultiColumnHeaderState == null;
            var headerState = CreateHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
            m_MultiColumnHeaderState = headerState;
				
            var multiColumnHeader = new MyMultiColumnHeader(headerState);
            if (firstInit)
                multiColumnHeader.ResizeToFit();
            m_TreeView = new NotifyTree(m_TreeViewState, multiColumnHeader, m_SelectedNotifyGroup);
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            m_Initialized = true;
        }

        private MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new[] 
			{
                new MultiColumnHeaderState.Column 
				{
					headerContent = new GUIContent("Name"),
					headerTextAlignment = TextAlignment.Left,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Center,
					width = 150, 
					minWidth = 60,
					autoResize = false,
					allowToggleVisibility = false
				},
				new MultiColumnHeaderState.Column 
				{
					headerContent = new GUIContent("IsOn"),
					headerTextAlignment = TextAlignment.Left,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Center,
					width = 60,
					minWidth = 60,
					autoResize = false
				},
				new MultiColumnHeaderState.Column 
				{
					headerContent = new GUIContent("Number"),
					headerTextAlignment = TextAlignment.Left,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Center,
					width = 60,
					minWidth = 60,
					autoResize = false,
					allowToggleVisibility = true
				},
				new MultiColumnHeaderState.Column 
				{
					headerContent = new GUIContent("Value"),
					headerTextAlignment = TextAlignment.Left,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Center,
					width = 60,
					minWidth = 60,
					autoResize = false
				}
			};

			Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");
            var state =  new MultiColumnHeaderState(columns);
			return state;
        }

        void OnEnable()
        {
            m_Initialized = false;
        }

        private void OnFocus()
        {
            m_Initialized = false;
        }

        void OnGUI ()
        {
            if(!Application.isPlaying)
            {
                EditorGUI.LabelField(multiColumnTreeViewRect, "This is only for use in the Playing mode");
                return;
            }
            InitIfNeeded();
            if (GUI.Button(refreshButtonRect, new GUIContent("Refresh")))
            {
                m_Initialized = false;
            }
            DrawNotifyGroupButtons(groupToolbarRect);
            SearchBar (toolbarRect);
            if(m_TreeView != null) m_TreeView.OnGUI(multiColumnTreeViewRect);
        }

        private void RefreshNotifyGroups()
        {
            var registeredTypes = NotifyManager.instance.GetNotifyGroupTypes();
            m_NotifyGroupTypes = registeredTypes == null ? new List<Type>() : new List<Type>(registeredTypes);
            if (m_SelectedNotifyGroup == null || !m_NotifyGroupTypes.Contains(m_SelectedNotifyGroup))
                m_SelectedNotifyGroup = m_NotifyGroupTypes.Count > 0 ? m_NotifyGroupTypes[0] : null;
        }

        private void DrawNotifyGroupButtons(Rect rect)
        {
            if (m_NotifyGroupTypes == null || m_NotifyGroupTypes.Count == 0)
            {
                EditorGUI.LabelField(rect, "No notification enum group is registered.");
                return;
            }

            var buttonRect = rect;
            for (var i = 0; i < m_NotifyGroupTypes.Count; i++)
            {
                var enumType = m_NotifyGroupTypes[i];
                var label = enumType == null ? "<null>" : enumType.Name;
                var width = Mathf.Max(80f, GUI.skin.button.CalcSize(new GUIContent(label)).x + 12f);
                buttonRect.width = Mathf.Min(width, rect.xMax - buttonRect.x);
                if (buttonRect.width <= 0) break;

                var selected = enumType == m_SelectedNotifyGroup;
                var oldColor = GUI.backgroundColor;
                if (selected) GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
                if (GUI.Button(buttonRect, label) && !selected)
                {
                    m_SelectedNotifyGroup = enumType;
                    m_Initialized = false;
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = oldColor;
                buttonRect.x += buttonRect.width + 4f;
            }
        }
        
        void SearchBar (Rect rect)
        {
            if (m_TreeView == null || m_SearchField == null) return;
            treeView.searchString = m_SearchField.OnGUI(rect, m_TreeView.searchString);
        }
        
        public static void ShowWindow ()
        {
            // 获取现有打开的窗口；如果没有，则新建一个窗口：
            var window = GetWindow<NotifyTreeViewWindow>();
            window.titleContent = new GUIContent("Notify Tree Window");
            window.Show();
        }
    }
}
#endif
