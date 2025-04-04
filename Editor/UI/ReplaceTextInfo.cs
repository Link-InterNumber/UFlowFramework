using UnityEngine;
using TMPro;


namespace PowerCellStudio
{
    public struct ReplaceTextInfo
    {
        public string textString;
        public FontStyle style;
        public float size;
        public TextAnchor alignment;
        public HorizontalWrapMode horizontal;
        public VerticalWrapMode vertical;
        public bool bestFit;
        public Color color;
        public bool raycast;
        // TextMeshProUGUI
        public bool autoSize;
        public VertexGradient colorGradient;
        public FontStyles meshProStyle;
        public TextAlignmentOptions meshProAlignment;
        public HorizontalAlignmentOptions meshProHorizontal;
        public VerticalAlignmentOptions meshProVertical;
    }

    public class TextNTextMeshProUGUISwitch
    {
        public static FontStyles ConvertFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Normal:
                    return FontStyles.Normal;
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }
        
        public static FontStyle ConvertFontStyle(FontStyles style)
        {
            if ((style & FontStyles.Bold) != 0 && (style & FontStyles.Italic) != 0)
            {
                return FontStyle.BoldAndItalic;
            }
            if ((style & FontStyles.Bold) != 0)
            {
                return FontStyle.Bold;
            }
            if ((style & FontStyles.Italic) != 0)
            {
                return FontStyle.Italic;
            }
            return FontStyle.Normal;
        }

        public static TextAlignmentOptions ConvertTextAnchor(TextAnchor textAnchor)
        {
            switch (textAnchor)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Midline;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.TopLeft;
            }
        }
        
        public static TextAnchor ConvertTextAlignmentOptions(TextAlignmentOptions textAlignmentOptions)
        {
            switch (textAlignmentOptions)
            {
                case TextAlignmentOptions.TopLeft:
                    return TextAnchor.UpperLeft;
                
                case TextAlignmentOptions.Top:
                    return TextAnchor.UpperCenter;
                
                case TextAlignmentOptions.TopRight:
                    return TextAnchor.UpperRight;
                
                case TextAlignmentOptions.MidlineLeft:
                case TextAlignmentOptions.Left:
                    return TextAnchor.MiddleLeft;
                
                case TextAlignmentOptions.Midline:
                case TextAlignmentOptions.Center:
                    return TextAnchor.MiddleCenter;
                
                case TextAlignmentOptions.MidlineRight:
                case TextAlignmentOptions.Right:
                    return TextAnchor.MiddleRight;

                case TextAlignmentOptions.BottomLeft:
                    return TextAnchor.LowerLeft;
                
                case TextAlignmentOptions.Bottom:
                    return TextAnchor.LowerCenter;
                
                case TextAlignmentOptions.BottomRight:
                    return TextAnchor.LowerRight;
                
                default:
                    return TextAnchor.UpperLeft;
            }
        }
    }
}