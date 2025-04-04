using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PowerCellStudio
{
    public static class StringExtension
    {
        #region string hash

        private static System.Security.Cryptography.SHA1 hash =
            new System.Security.Cryptography.SHA1CryptoServiceProvider();

        public static int GenHashCode(this string str)
        {
            int hashCode = 0;
            if (string.IsNullOrEmpty(str)) return hashCode;
            var bytes = Encoding.Unicode.GetBytes(str);
            byte[] hashText = hash.ComputeHash(bytes);
            int hashCodeStart = BitConverter.ToInt32(hashText, 0);
            int hashCodeMedium = BitConverter.ToInt32(hashText, 8);
            int hashCodeEnd = BitConverter.ToInt32(hashText, 16);
            hashCode = (hashCodeStart * 31 + hashCodeMedium) * 17 + hashCodeEnd;
            return int.MaxValue - hashCode;
        }
        
        #endregion

        #region KMP
        
        static int[] ComputePrefixFunction(string pattern)
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
        
        static int[] KMPAlgorithm(string text, string pattern, int[] prefixFunction)
        {
            int n = text.Length;
            int m = pattern.Length;
            if (n - m + 1 <= 0) return Array.Empty<int>();
            int[] result = new int[n - m + 1];
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
                    result[i - m + 1] = i;
                    k = prefixFunction[k - 1];
                }
            }
            return result;
        }

        /// <summary>
        /// 使用 KMP 算法查找模式在文本中的所有出现位置。
        /// </summary>
        /// <param name="text">要搜索的文本。</param>
        /// <param name="pattern">要搜索的模式。</param>
        /// <param name="result">一个数组，表示模式在文本中出现的起始索引。</param>
        public static void KMPIndexOf(string text, string pattern, out int[] result)
        {
            int[] prefixFunction = ComputePrefixFunction(pattern);
            result = KMPAlgorithm(text, pattern, prefixFunction);
        }
        
        #endregion

        #region Set Color
        
        /// <summary>
        /// 将文本中匹配正则表达式的部分设置为指定的颜色（十六进制格式）。
        /// </summary>
        /// <param name="text">要处理的文本。</param>
        /// <param name="pattern">用于匹配文本的正则表达式。</param>
        /// <param name="colorInHex">指定的颜色（十六进制格式）。</param>
        /// <returns>处理后的文本，匹配的部分被设置为指定的颜色。</returns>
        public static string SetColor(this string text, Regex pattern, string colorInHex)
        {
            if(!colorInHex.StartsWith("#")) colorInHex = "#" + colorInHex;
            var matched = pattern.Matches(text);
            if (matched.Count == 0) return text;
            var enumeratorIndex = 0;
            var  result = pattern.Replace(text, o =>
            {
                var re = $"<color={colorInHex}>{matched[enumeratorIndex].Value}</color>";
                enumeratorIndex++;
                return re;
            });
            return result;
        }
        
        /// <summary>
        /// 将文本中匹配正则表达式的部分设置为指定的颜色（Color 对象）。
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
        /// 将文本中的数字部分设置为指定的颜色。
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
                LinkLog.LogError(e.ToString());
                return format;
            }
        }

        public static (int item1, int item2) ToI2(this string input)
        {
            if (string.IsNullOrEmpty(input)) return default;
            var split = input.Split(';');
            if (split.Length < 2) return default;
            if (int.TryParse(split[0], out var number1)
                && int.TryParse(split[0], out var number2))
            {
                return (number1, number2);
            }
            return default;
        }
    }
}