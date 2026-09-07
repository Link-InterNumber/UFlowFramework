using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    /// <summary>
    /// Editor 工具通用的 UI 尺寸、颜色和基础控件样式。
    /// Shared UI dimensions, colors, and base control styles for Editor tools.
    /// </summary>
    public static class EditorUIStyle
    {
        public const float RootPaddingLeft = 8f;
        public const float RootPaddingRight = 8f;
        public const float RootPaddingTop = 6f;
        public const float RootPaddingBottom = 8f;
        public const float HeaderBottomMargin = 6f;
        public const float ToolbarHorizontalPadding = 6f;
        public const float ToolbarVerticalPadding = 4f;
        public const float ToolbarHeight = 28f;
        public const float PanelPadding = 8f;
        public const float ResizerWidth = 8f;
        public const float ToolbarButtonWidth = 70f;
        public const float AutoRefreshButtonWidth = 90f;
        public const float StatisticLabelWidth = 90f;
        public const float CountLabelWidth = 36f;
        public const float GroupSpacing = 6f;
        public const float SectionSpacing = 4f;
        public const float TabHeight = 25f;
        public const float TabMaxWidth = 150f;
        public const float TabSpacing = 5f;
        public const float TabRowSpacing = 5f;
        public const float SaveButtonHeight = 26f;
        public const float ActionButtonHeight = 30f;
        public const float SecondaryButtonHeight = 26f;
        public const float ContentSpacing = 8f;
        public const float HeaderTitleSize = 18f;
        public const float TreeRowHeight = 20f;
        public const float TreeToggleWidth = 20f;
        public const float TreeViewBottomReserve = 60f;
        public const float TreeViewMaxLayoutSize = 100000f;
        public const float TreeFolderColumnWidth = 200f;
        public const float TreeFolderColumnMinWidth = 100f;
        public const float TreeStatusColumnWidth = 60f;
        public const float TreeContentColumnWidth = 100f;
        public const float DestructiveButtonHeight = 26f;
        public const float ManifestCardSpacing = 6f;
        public const float ManifestCardHeightPadding = 20f;
        public const float ManifestScrollReserve = 20f;
        public const float NodeIndentWidth = 18f;
        public const float MinNodeNameWidth = 110f;
        public const float MaxNodeNameWidth = 260f;
        public const float MinComponentToggleWidth = 86f;
        public const float MaxComponentToggleWidth = 170f;

        public static readonly Color RootBackground = new Color(0.12f, 0.12f, 0.12f, 1f);
        public static readonly Color PanelBackground = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        public static readonly Color SummaryBackground = new Color(0.18f, 0.22f, 0.28f, 0.9f);
        public static readonly Color GraphBackground = new Color(0.2f, 0.22f, 0.26f, 1f);
        public static readonly Color ResizerBackground = new Color(0.22f, 0.25f, 0.3f, 1f);
        public static readonly Color SeparatorColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public static readonly Color TitleColor = new Color(0.88f, 0.92f, 0.98f, 1f);
        public static readonly Color MutedTextColor = new Color(0.62f, 0.66f, 0.72f, 1f);
        public static readonly Color SummaryTextColor = new Color(0.82f, 0.88f, 0.96f, 1f);
        public static readonly Color DetailTextColor = new Color(0.78f, 0.82f, 0.9f, 1f);
        public static readonly Color AssetTextColor = new Color(0.82f, 0.86f, 0.94f, 1f);
        public static readonly Color ImguiPanelBackground = new Color(0.16f, 0.17f, 0.2f, 0.95f);
        public static readonly Color ImguiSectionBackground = new Color(0.2f, 0.22f, 0.26f, 0.95f);
        public static readonly Color ImguiLinkColor = new Color(0.45f, 0.72f, 1f, 1f);
        public static readonly Color ImguiBorderColor = new Color(0.3f, 0.32f, 0.36f, 0.7f);
        public static readonly Color AccentColor = new Color(0.22f, 0.52f, 0.86f, 1f);
        public static readonly Color AccentHoverColor = new Color(0.3f, 0.62f, 1f, 1f);

        private static GUIStyle _windowBackground;
        private static GUIStyle _panelBox;
        private static GUIStyle _sectionBox;
        private static GUIStyle _titleLabel;
        private static GUIStyle _mutedLabel;
        private static GUIStyle _linkLabel;
        private static GUIStyle _windowTitle;
        private static GUIStyle _tabButton;
        private static GUIStyle _primaryButton;
        private static GUIStyle _destructiveButton;
        private static GUIStyle _manifestCard;
        private static GUIStyle _fieldLabel;
        private static GUIStyle _fieldValue;
        private static GUIStyle _headerTitle;
        private static GUIStyle _sectionTitle;
        private static GUIStyle _richTextLabel;

        public static GUIStyle WindowBackground => _windowBackground ?? (_windowBackground = CreateBackgroundStyle(RootBackground));
        public static GUIStyle PanelBox => _panelBox ?? (_panelBox = CreateBackgroundStyle(ImguiPanelBackground));
        public static GUIStyle SectionBox => _sectionBox ?? (_sectionBox = CreateBackgroundStyle(ImguiSectionBackground));
        public static GUIStyle TitleLabel => _titleLabel ?? (_titleLabel = CreateLabelStyle(TitleColor, FontStyle.Bold));
        public static GUIStyle MutedLabel => _mutedLabel ?? (_mutedLabel = CreateLabelStyle(MutedTextColor, FontStyle.Normal));
        public static GUIStyle LinkLabel => _linkLabel ?? (_linkLabel = CreateLinkStyle());
        public static GUIStyle WindowTitle => _windowTitle ?? (_windowTitle = CreateTitleStyle());
        public static GUIStyle TabButton => _tabButton ?? (_tabButton = CreateTabButtonStyle());
        public static GUIStyle PrimaryButton => _primaryButton ?? (_primaryButton = CreatePrimaryButtonStyle());
        public static GUIStyle DestructiveButton => _destructiveButton ?? (_destructiveButton = CreateDestructiveButtonStyle());
        public static GUIStyle ManifestCard => _manifestCard ?? (_manifestCard = CreateBackgroundStyle(ImguiSectionBackground));
        public static GUIStyle HeaderTitle => _headerTitle ?? (_headerTitle = CreateHeaderTitleStyle());
        public static GUIStyle SectionTitle => _sectionTitle ?? (_sectionTitle = CreateSectionTitleStyle());
        public static GUIStyle RichTextLabel => _richTextLabel ?? (_richTextLabel = CreateRichTextLabelStyle());

        public static void DrawImguiHeader(string title, string subtitle)
        {
            using (new EditorGUILayout.VerticalScope(PanelBox))
            {
                EditorGUILayout.LabelField(title, HeaderTitle);
                if (!string.IsNullOrEmpty(subtitle))
                {
                    EditorGUILayout.LabelField(subtitle, MutedLabel);
                }
            }

            GUILayout.Space(ContentSpacing);
        }

        public static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, SeparatorColor);
        }

        public static void ApplyRoot(VisualElement root)
        {
            if (root == null)
                return;

            root.style.paddingLeft = RootPaddingLeft;
            root.style.paddingRight = RootPaddingRight;
            root.style.paddingTop = RootPaddingTop;
            root.style.paddingBottom = RootPaddingBottom;
            root.style.backgroundColor = RootBackground;
            root.style.flexDirection = FlexDirection.Column;
        }

        public static VisualElement CreateHeader(string titleText, string subtitleText)
        {
            var header = new VisualElement();
            header.style.marginBottom = HeaderBottomMargin;

            var title = new Label(titleText);
            title.style.fontSize = 16f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = TitleColor;
            header.Add(title);

            var subtitle = new Label(subtitleText);
            subtitle.style.fontSize = 11f;
            subtitle.style.marginTop = 2f;
            subtitle.style.color = MutedTextColor;
            header.Add(subtitle);
            return header;
        }

        public static void ApplyToolbar(UnityEditor.UIElements.Toolbar toolbar, float bottomMargin = 6f)
        {
            if (toolbar == null)
                return;

            toolbar.style.paddingLeft = ToolbarHorizontalPadding;
            toolbar.style.paddingRight = ToolbarHorizontalPadding;
            toolbar.style.paddingTop = ToolbarVerticalPadding;
            toolbar.style.paddingBottom = ToolbarVerticalPadding;
            toolbar.style.marginBottom = bottomMargin;
            toolbar.style.height = ToolbarHeight;
        }

        public static void ApplyPanel(VisualElement panel)
        {
            if (panel == null)
                return;

            panel.style.backgroundColor = PanelBackground;
        }

        public static void ApplySummary(Label summary)
        {
            if (summary == null)
                return;

            summary.style.paddingLeft = PanelPadding;
            summary.style.paddingRight = PanelPadding;
            summary.style.paddingTop = 6f;
            summary.style.paddingBottom = 6f;
            summary.style.marginBottom = HeaderBottomMargin;
            summary.style.backgroundColor = SummaryBackground;
            summary.style.color = SummaryTextColor;
        }

        public static void ApplyResizer(VisualElement resizer)
        {
            if (resizer == null)
                return;

            resizer.style.width = ResizerWidth;
            resizer.style.minWidth = ResizerWidth;
            resizer.style.maxWidth = ResizerWidth;
            resizer.style.flexShrink = 0f;
            resizer.style.backgroundColor = ResizerBackground;
        }

        public static void DrawWindowBackground(Rect windowRect)
        {
            EditorGUI.DrawRect(windowRect, RootBackground);
        }

        private static GUIStyle CreateBackgroundStyle(Color color)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { background = CreateColorTexture(color) },
                padding = new RectOffset(PanelPaddingToInt(), PanelPaddingToInt(), 6, 6),
                margin = new RectOffset(0, 0, 0, 0)
            };
            return style;
        }

        private static GUIStyle CreateLabelStyle(Color color, FontStyle fontStyle)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                fontStyle = fontStyle,
                normal = { textColor = color }
            };
            return style;
        }

        private static GUIStyle CreateLinkStyle()
        {
            var style = new GUIStyle(EditorStyles.linkLabel)
            {
                normal = { textColor = ImguiLinkColor },
                hover = { textColor = Color.white }
            };
            return style;
        }

        private static GUIStyle CreateTitleStyle()
        {
            var style = CreateLabelStyle(TitleColor, FontStyle.Bold);
            style.fontSize = 25;
            style.margin = new RectOffset(0, 0, 4, 6);
            return style;
        }

        private static GUIStyle CreateTabButtonStyle()
        {
            var style = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = TabHeight,
                padding = new RectOffset(8, 8, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };
            return style;
        }

        private static GUIStyle CreatePrimaryButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = SaveButtonHeight,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = CreateColorTexture(AccentColor), textColor = Color.white },
                hover = { background = CreateColorTexture(AccentHoverColor), textColor = Color.white },
                active = { background = CreateColorTexture(new Color(0.16f, 0.4f, 0.72f, 1f)), textColor = Color.white }
            };
            return style;
        }

        private static GUIStyle CreateDestructiveButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = DestructiveButtonHeight,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = CreateColorTexture(new Color(0.42f, 0.16f, 0.16f, 1f)), textColor = Color.white },
                hover = { background = CreateColorTexture(new Color(0.58f, 0.2f, 0.2f, 1f)), textColor = Color.white },
                active = { background = CreateColorTexture(new Color(0.3f, 0.1f, 0.1f, 1f)), textColor = Color.white }
            };
            return style;
        }

        private static Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "EditorUIStyle.Background"
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static GUIStyle CreateHeaderTitleStyle()
        {
            var style = CreateLabelStyle(TitleColor, FontStyle.Bold);
            style.fontSize = Mathf.RoundToInt(HeaderTitleSize);
            style.margin = new RectOffset(0, 0, 1, 3);
            return style;
        }

        private static GUIStyle CreateSectionTitleStyle()
        {
            var style = CreateLabelStyle(TitleColor, FontStyle.Bold);
            style.fontSize = 12;
            style.margin = new RectOffset(0, 0, 2, 4);
            return style;
        }

        private static GUIStyle CreateRichTextLabelStyle()
        {
            var style = CreateLabelStyle(DetailTextColor, FontStyle.Normal);
            style.richText = true;
            style.wordWrap = true;
            style.padding = new RectOffset(4, 4, 4, 4);
            return style;
        }

        private static int PanelPaddingToInt()
        {
            return Mathf.RoundToInt(PanelPadding);
        }
    }
}
