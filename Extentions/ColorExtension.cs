using System;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public static class ColorExtension
    {
        /// <summary>
        /// 将Color对象格式化为十六进制字符串。
        /// Converts a Color object to a hexadecimal string.
        /// </summary>
        /// <param name="color">要转换为十六进制字符串的颜色。</param>
        /// <returns>表示颜色的十六进制字符串。</returns>
        public static string FormatHex(this Color color)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("#");
            stringBuilder.Append(((byte)(color.r * 255)).ToString("X2"));
            stringBuilder.Append(((byte)(color.g * 255)).ToString("X2"));
            stringBuilder.Append(((byte)(color.b * 255)).ToString("X2"));
            stringBuilder.Append(((byte)(color.a * 255)).ToString("X2"));
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 从十六进制字符串解析Color对象。
        /// Parses a Color object from a hexadecimal string.
        /// </summary>
        /// <param name="hex">表示颜色的十六进制字符串。</param>
        /// <returns>解析得到的Color对象。</returns>
        public static Color ParseHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                Debug.LogError("Hex string is null or empty / 十六进制字符串为空");
                return Color.white;
            }

            ReadOnlySpan<char> span = hex.AsSpan();
            if (span[0] == '#')
                span = span.Slice(1);

            if (span.Length != 6 && span.Length != 8)
            {
                Debug.LogError("Invalid hex string length / 十六进制字符串长度无效");
                return Color.white;
            }

            try
            {
                byte r = byte.Parse(span.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(span.Slice(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(span.Slice(4, 2), System.Globalization.NumberStyles.HexNumber);
                byte a = span.Length == 8 ? byte.Parse(span.Slice(6, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255;

                return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse hex string '{hex}': {e.Message} / 解析十六进制字符串失败");
                return Color.white;
            }
        }
    }
}