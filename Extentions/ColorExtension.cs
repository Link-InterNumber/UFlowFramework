using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public static class   ColorExtension
    {
        public static string FormatHex(this Color color)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("#");
            stringBuilder.Append(((byte) (color.r * 255)).ToString("X2"));
            stringBuilder.Append(((byte) (color.g * 255)).ToString("X2"));
            stringBuilder.Append(((byte) (color.b * 255)).ToString("X2"));
            stringBuilder.Append(((byte) (color.a * 255)).ToString("X2"));
            return stringBuilder.ToString();
        }
        
        public static Color ParseHex(string hex)
        {
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }
            if (hex.Length < 6)
            {
                Debug.LogError("Invalid hex string length");
                return Color.white;
            }
            if (hex.Length < 8)
            {
                hex += "FF";
            }
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }
    }
}