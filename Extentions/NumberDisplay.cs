using System;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public static class NumberDisplay
    {
        /// <summary>
        /// 将整数格式化为带符号的英文缩写形式。
        /// Formats the integer as an English shorthand representation with a sign.
        /// </summary>
        /// <param name="num">要格式化的数字。</param>
        /// <param name="size">小数位数。</param>
        /// <returns>带符号的格式化字符串。</returns>
        public static string FormatNumberEnSign(this int num, int size = 2)
        {
            return num < 0 
                ? "-" + FormatNumberEn(Mathf.Abs(num), size) 
                : FormatNumberEn(Mathf.Abs(num), size);
        }
        
        /// <summary>
        /// 将百万数字转化为百分比率。
        /// Converts millions to rate percentage.
        /// </summary>
        /// <param name="num">要转换的数字。</param>
        /// <returns>代表率的双精度数。</returns>
        public static double MillionToRate(this int num)
        {
            return num * 0.0001D;
        }

        /// <summary>
        /// 将数字格式化为英文缩写表示。
        /// Formats the number as English shorthand representation.
        /// </summary>
        /// <param name="num">要格式化的数字。</param>
        /// <param name="size">小数位数。</param>
        /// <returns>格式化后的字符串。</returns>
        public static string FormatNumberEn(this long num, int size = 2)
        {
            var result = new StringBuilder();
            if (num >= 1000000000L)
            {
                result.Append((num * 0.000000001f).ToString($"N{size}") + "B");
                return result.ToString();
            }
            if (num >= 1000000L)
            {
                result.Append((num * 0.000001f).ToString($"N{size}") + "M");
                return result.ToString();
            }
            if (num >= 1000L)
            {
                result.Append((num * 0.001f).ToString($"N{size}") + "K");
                return result.ToString();
            }
            return result.Append(num.ToString("N0")).ToString();
        }
        
        /// <summary>
        /// 将整数格式化为带符号的中文缩写形式。
        /// Formats the integer as a Chinese shorthand representation with a sign.
        /// </summary>
        /// <param name="num">要格式化的数字。</param>
        /// <param name="size">小数位数。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <returns>带符号的格式化字符串。</returns>
        public static string FormatNumberCnSign(this int num, int size = 2, bool isTraditional = false)
        {
            return num < 0
                ? "-" + FormatNumberCn(Mathf.Abs(num), size, isTraditional)
                : FormatNumberCn(Mathf.Abs(num), size, isTraditional);
        }
        
        /// <summary>
        /// 将数字格式化为中文缩写表示。
        /// Formats the number as Chinese shorthand representation.
        /// </summary>
        /// <param name="num">要格式化的数字。</param>
        /// <param name="size">小数位数。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <returns>格式化后的字符串。</returns>
        public static string FormatNumberCn(this long num, int size = 2, bool isTraditional = false)
        {
            var result = new StringBuilder();
            if (num >= 100000000)
            {
                result.Append((num * 0.00000001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
                return result.ToString();
            }
            if (num >= 10000)
            {
                result.Append((num * 0.0001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
                return result.ToString();
            }
            result.Append(num.ToString("N0"));
            return result.ToString();
        }

        /// <summary>
        /// 获取数字的中文单位。
        /// Gets the Chinese unit of a number.
        /// </summary>
        /// <param name="number">要获取单位的数字。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <returns>数字的单位字符串。</returns>
        public static string GetNumberUnitCn(this long number, bool isTraditional)
        {
            if (number >= 100000000)
            {
                return "亿";
            }
            if (number >= 10000)
            {
                return "万";
            }
            return string.Empty;
        }

        /// <summary>
        /// 格式化数字为中英文缩写表示。
        /// Formats the number as Chinese or English shorthand representation.
        /// </summary>
        /// <param name="num">要格式化的数字。</param>
        /// <param name="isChinese">是否使用中文。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <param name="size">小数位数。</param>
        /// <returns>格式化后的字符串。</returns>
        public static string FormatNumber(this long num, bool isChinese, bool isTraditional = false, int size = 2)
        {
            return isChinese ? FormatNumberCn(num, size, isTraditional) : FormatNumberEn(num, size);
        }
        
        /// <summary>
        /// 将索引格式化为中文。
        /// Formats the index as a Chinese string.
        /// </summary>
        /// <param name="index">要格式化的索引。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <returns>格式化后的索引字符串。</returns>
        public static string FormatIndexCn(this long index, bool isTraditional = false)
        {
            StringBuilder result = new StringBuilder();
            if (index >= 100000000)
            {
                result.Append(IntToChineseHandler(Mathf.FloorToInt(index * 0.00000001f), isTraditional));
                result.Append(GetNumberUnitCn(index, isTraditional));
                index %= 100000000;
            }
            if (index >= 10000)
            {
                result.Append(IntToChineseHandler(Mathf.FloorToInt(index * 0.0001f), isTraditional));
                result.Append(GetNumberUnitCn(index, isTraditional));
                index %= 10000;
            }
            if (index > 0)
            {
                string remaining = IntToChineseHandler(index, isTraditional);
                if (!string.IsNullOrEmpty(remaining))
                    result.Append(remaining);
            }
            return result.ToString();
        }

        /// <summary>
        /// 将数字转化为中文字符，仅处理0~10。
        /// Converts integers from 0 to 10 into Chinese characters.
        /// </summary>
        /// <param name="num">0~10的数字。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <returns>对应的中文字符。</returns>
        public static string IntToChineseHandler(long num, bool isTraditional)
        {
            return num switch
            {
                0 => isTraditional ? "〇" : "零",
                1 => isTraditional ? "壹" : "一",
                2 => isTraditional ? "贰" : "二",
                3 => isTraditional ? "叁" : "三",
                4 => isTraditional ? "肆" : "四",
                5 => isTraditional ? "伍" : "五",
                6 => isTraditional ? "陆" : "六",
                7 => isTraditional ? "柒" : "七",
                8 => isTraditional ? "捌" : "八",
                9 => isTraditional ? "玖" : "九",
                10 => isTraditional ? "拾" : "十",
                _ => "输入数字超了",
            };
        }
    }
}