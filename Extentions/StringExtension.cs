using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Security.Cryptography;

namespace PowerCellStudio
{
    public static class StringExtension
    {
        #region string hash

        /// <summary>
        /// 生成字符串的哈希代码。
        /// Generates a hash code for the given string.
        /// </summary>
        /// <param name="str">要生成哈希代码的字符串。</param>
        /// <returns>生成的哈希代码。</returns>
        public static int GenHashCode(this string str)
        {
            int hashCode = 0;
            if (string.IsNullOrEmpty(str)) return hashCode;
            var bytes = Encoding.Unicode.GetBytes(str);
            using (var hash = SHA1.Create())
            {
                byte[] hashText = hash.ComputeHash(bytes);
                int hashCodeStart = BitConverter.ToInt32(hashText, 0);
                int hashCodeMedium = BitConverter.ToInt32(hashText, 8);
                int hashCodeEnd = BitConverter.ToInt32(hashText, 16);
                hashCode = (hashCodeStart * 31 + hashCodeMedium) * 17 + hashCodeEnd;
            }
            return int.MaxValue - hashCode;
        }
        
        #endregion

        #region KMP

        /// <summary>
        /// 计算前缀函数，用于KMP算法。
        /// Computes prefix function for KMP algorithm.
        /// </summary>
        /// <param name="pattern">要查找的模式。</param>
        /// <returns>前缀函数数组。</returns>
        private static int[] ComputePrefixFunction(string pattern)
        {
            int m = pattern.Length;
            int[] prefixFunction = new int[m];
            prefixFunction[0] = 0;
            int k = 0;
            for (int q = 1; q < m; q++)
            {
                while (k > 0 && pattern[k] != pattern[q])
                {
                    k = prefixFunction[k - 1];
                }
                if (pattern[k] == pattern[q])
                {
                    k++;
                }
                prefixFunction[q] = k;
            }
            return prefixFunction;
        }

        /// <summary>
        /// 使用 KMP 算法查找模式在文本中的所有出现位置。
        /// Finds all occurrences of the pattern in the text using KMP algorithm.
        /// </summary>
        /// <param name="text">要搜索的文本。</param>
        /// <param name="pattern">要搜索的模式。</param>
        /// <param name="result">数组，表示模式在文本中出现的起始索引。</param>
        public static void KMPIndexOf(string text, string pattern, out int[] result)
        {
            int[] prefixFunction = ComputePrefixFunction(pattern);
            result = KMPAlgorithm(text, pattern, prefixFunction);
        }
        
        private static int[] KMPAlgorithm(string text, string pattern, int[] prefixFunction)
        {
            int n = text.Length;
            int m = pattern.Length;
            var indices = new List<int>(); // This will hold the index positions
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                while (k > 0 && pattern[k] != text[i])
                {
                    k = prefixFunction[k - 1];
                }
                if (pattern[k] == text[i])
                {
                    k++;
                }
                if (k == m)
                {
                    indices.Add(i - m + 1); // Append current match start index
                    k = prefixFunction[k - 1];
                }
            }
            return indices.ToArray(); // Convert list to array before returning
        }

        #endregion

        #region Set Color

        /// <summary>
        /// 将文本中匹配正则表达式的部分设置为指定的颜色（十六进制格式）。
        /// Sets parts of the text matched by regex to a specified color in hexadecimal encoding.
        /// </summary>
        /// <param name="text">要处理的文本。</param>
        /// <param name="pattern">用于匹配文本的正则表达式。</param>
        /// <param name="colorInHex">指定的颜色（十六进制格式）。</param>
        /// <returns>处理后的文本，匹配的部分被设置为指定的颜色。</returns>
        public static string SetColor(this string text, Regex pattern, string colorInHex)
        {
            if (string.IsNullOrEmpty(text) || pattern == null) return text;
            if (!colorInHex.StartsWith("#")) colorInHex = "#" + colorInHex;

            ReadOnlySpan<char> span = text.AsSpan();
            var matches = pattern.Matches(text);
            if (matches.Count == 0) return text;

            int lastIndex = 0;
            var sb = new System.Text.StringBuilder(text.Length + matches.Count * 20);

            foreach (Match match in matches)
            {
                // Append text before match
                sb.Append(span.Slice(lastIndex, match.Index - lastIndex));
                // Append colored match
                sb.Append("<color=");
                sb.Append(colorInHex);
                sb.Append('>');
                sb.Append(span.Slice(match.Index, match.Length));
                sb.Append("</color>");
                lastIndex = match.Index + match.Length;
            }
            // Append the rest
            sb.Append(span.Slice(lastIndex));
            return sb.ToString();
        }

        /// <summary>
        /// 将文本中匹配正则表达式的部分设置为指定的颜色（Color 对象）。
        /// Sets parts of the text matched by regex to a specified color as a Color object.
        /// </summary>
        /// <param name="text">要处理的文本。</param>
        /// <param name="pattern">用于匹配文本的正则表达式。</param>
        /// <param name="color">指定的颜色（Color 对象）。</param>
        /// <returns>处理后的文本，匹配的部分被设置为指定的颜色。</returns>
        public static string SetColor(this string text, Regex pattern, Color color)
        {
            var colorHex = color.FormatHex();
            return SetColor(text, pattern, colorHex);
        }

        /// <summary>
        /// 将文本中的特点部分设置为指定的颜色。
        /// Sets the pattern of the text to a specified color.
        /// </summary>
        /// <param name="text">要处理的文本。</param>
        /// <param name="pattern">要查找的文本。</param>
        /// <param name="color">指定的颜色（Color 对象）。</param>
        /// <returns>处理后的文本，数字部分被设置为指定的颜色。</returns>
        public static string ColorText(this string text, string pattern, Color color)
        {
            string pattern = Regex.Escape(input);
            var regex = new Regex(pattern);
            return SetColor(text, regex, color);   
        }

        /// <summary>
        /// 将文本中的数字部分设置为指定的颜色。
        /// Sets the numeric parts of the text to a specified color.
        /// </summary>
        /// <param name="text">要处理的文本。</param>
        /// <param name="color">指定的颜色（Color 对象）。</param>
        /// <returns>处理后的文本，数字部分被设置为指定的颜色。</returns>
        public static string ColorNumber(this string text, Color color)
        {
            var numberRegex = new Regex(@"(\-|\+)?\d+(\.\d+)?(\%)?");
            return SetColor(text, numberRegex, color);   
        }
        
        #endregion

        /// <summary>
        /// 安全格式化字符串。
        /// Formats the string safely, returning the original string on failure.
        /// </summary>
        /// <param name="format">要格式化的字符串。</param>
        /// <param name="args">格式化字符串的参数。</param>
        /// <returns>格式化后的字符串，如果格式化失败则返回原始字符串。</returns>
        public static string SafeFormat(this string format, params object[] args)
        {
            try
            {
                return string.Format(format, args);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString()); // Ensure Debug.LogError is used for logging
                return format;
            }
        }

        /// <summary>
        /// 将分号分隔的字符串转换为包含两个整数的元组。
        /// Converts a semicolon-separated string into a tuple containing two integers.
        /// </summary>
        /// <param name="input">要转换的字符串 / The string to convert.</param>
        /// <returns>包含两个整数的元组。如果转换失败则返回 (0, 0)。/ A tuple containing two integers. Returns (0, 0) if conversion fails.</returns>
        public static (int item1, int item2) ToI2(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return default;

            ReadOnlySpan<char> span = input.AsSpan();
            int sepIndex = span.IndexOf(';');
            if (sepIndex < 1 || sepIndex == span.Length - 1) return default;

            var first = span.Slice(0, sepIndex).Trim();
            var second = span.Slice(sepIndex + 1).Trim();

            if (int.TryParse(first, out var number1) && int.TryParse(second, out var number2))
            {
                return (number1, number2);
            }
            return default;
        }
    }
}