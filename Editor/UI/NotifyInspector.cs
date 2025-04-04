#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace PowerCellStudio
{
    [Serializable]
//TreeElement 数据类已经过扩展以便保存额外数据，您可以在前端 TreeView 中显示和编辑这些数据。
    public class NotifyTreeElement : TreeViewItem
    {
        public NotifyType notifyType;
        public bool isOn;
        public int notifyNumber;
        public int notifyValue;
    }

    public class NotifyTree : TreeView
    {
        private float kRowHeights = 20f;
        private float kToggleWidth = 20f;

        public NotifyTree(TreeViewState state) : base(state)
        {
            if(!Application.isPlaying) return;
            Reload();
        }

        public NotifyTree(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            rowHeight = 20;
            // columnIndexForTreeFoldouts = 2;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; 
            // extraSpaceBeforeIconAndLabel = kToggleWidth;
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
                    toggleRect.width = kToggleWidth;
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
            NotifyManager.instance.GetNotifyInfo(NotifyType.Root, out var isOn, out var notifyNumber, out var notifyValue);
            var id = 0;
            var root = new NotifyTreeElement()
            {
                notifyType = NotifyType.Root,
                isOn = isOn,
                notifyNumber = notifyNumber,
                notifyValue = notifyValue,
                id = id,
                depth = -1,
                displayName = Enum.GetName(typeof(NotifyType), NotifyType.Root)
            };
            id++;
            if(!Application.isPlaying) return root;
            var allTypes = Enum.GetValues(typeof(NotifyType)) as  NotifyType[];
            var childrenNode = new List<NotifyTreeElement>();
            for (var i = 0; i < allTypes.Length; i++)
            {
                var  type = allTypes[i];
                if (type == NotifyType.Root) continue;
                if (NotifyManager.instance.GetParent(type) != NotifyType.Root) continue;
                NotifyManager.instance.GetNotifyInfo(type, out bool On, out var N, out var V);
                var node = new NotifyTreeElement()
                {
                    notifyType = type,
                    isOn = On,
                    notifyNumber = N,
                    notifyValue = V,
                    id = id,
                    displayName = Enum.GetName(typeof(NotifyType), type)
                };
                root.AddChild(node);
                childrenNode.Add(node);
                id++;
            }

            while (childrenNode.Count > 0)
            {
                var executeList = new List<NotifyTreeElement>(childrenNode);
                childrenNode.Clear();
                for (var i = 0; i < executeList.Count; i++)
                {
                    var parent = executeList[i];
                    var childrenType = NotifyManager.instance.GetChildren(parent.notifyType);
                    foreach (var notifyType in childrenType)
                    {
                        NotifyManager.instance.GetNotifyInfo(notifyType, out bool On, out var N, out var V);
                        var node = new NotifyTreeElement()
                        {
                            notifyType = notifyType,
                            isOn = On,
                            notifyNumber = N,
                            notifyValue = V,
                            id = id,
                            displayName = Enum.GetName(typeof(NotifyType), notifyType)
                        };
                        parent.AddChild(node);
                        childrenNode.Add(node);
                        id++;
                    }
                }
            }
            SetupDepthsFromParentsAndChildren(root);
            return root;
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

        public NotifyTree treeView
        {
            get { return m_TreeView; }
        }
        
        Rect multiColumnTreeViewRect
        {
            get { return new Rect(20, 30, position.width-40, position.height-60); }
        }

        Rect refreshButtonRect
        {
            get { return new Rect(20, 10, 60, 20); }
        }
        
        Rect toolbarRect
        {
            get { return new Rect (90f, 10f, position.width-110f, 20f); }
        }
        
        void InitIfNeeded ()
        {
            if (m_Initialized) return;
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
            m_TreeView = new NotifyTree(m_TreeViewState, multiColumnHeader);
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
            SearchBar (toolbarRect);
            if(m_TreeView != null) m_TreeView.OnGUI(multiColumnTreeViewRect);
        }
        
        void SearchBar (Rect rect)
        {
            treeView.searchString = m_SearchField.OnGUI(rect, m_TreeView.searchString);
        }
        
        // 将名为 "My Window" 的菜单添加到 Window 菜单
        [MenuItem ("Tools/Notify TreeView Window")]
        public static void ShowWindow ()
        {
            // 获取现有打开的窗口；如果没有，则新建一个窗口：
            if (Enum.GetValues(typeof(NotifyType)).Length <= 1) return;
            var window = GetWindow<NotifyTreeViewWindow>();
            window.titleContent = new GUIContent("Notify Tree Window");
            window.Show();
        }
    }
}
#endif
