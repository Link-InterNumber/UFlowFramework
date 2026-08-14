using System;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public static class NumberDisplay
    {
        
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
            if (num < 0)
            {
                result.Append("-");
                num = -num;
            } 
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
            if (num < 0)
            {
                result.Append("负");
                num = -num;
            }
            if (num >= 1000000000000L)
            {
                result.Append((num * 0.000000000001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
                return result.ToString();
            }
            if (num >= 100000000L)
            {
                result.Append((num * 0.00000001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
                return result.ToString();
            }
            if (num >= 10000L)
            {
                result.Append((num * 0.0001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
                return result.ToString();
            }
            result.Append(num.ToString());
            return result.ToString();
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
        /// 格式化数字为中英文表示。
        /// Formats the number as Chinese or English shorthand representation.
        /// </summary>
        /// <param name="num">要格式化的数字。</param>
        /// <param name="isChinese">是否使用中文。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <param name="size">小数位数。</param>
        /// <returns>格式化后的字符串。</returns>
        public static string FormatIndex(this long num, bool isChinese, bool isTraditional = false, int size = 2)
        {
            return isChinese ? FormatIndexCn(num, isTraditional, size) : num.ToString("N0");
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
            if (number >= 1000000000000L)
                return "兆";
            if (number >= 100000000L)
                return isTraditional ? "億" : "亿";
            if (number >= 10000L)
                return isTraditional ? "萬" : "万";
            if (number >= 1000L)
                return isTraditional ? "仟" : "千";
            if (number >= 100L)
                return isTraditional ? "佰" : "百";
            if (number >= 10)
                return isTraditional ? "拾" : "十";
            return string.Empty;
        }
        
        /// <summary>
        /// 将索引格式化为中文。
        /// Formats the index as a Chinese string.
        /// </summary>
        /// <param name="index">要格式化的索引。</param>
        /// <param name="isTraditional">是否使用繁体中文。</param>
        /// <returns>格式化后的索引字符串。</returns>
        public static string FormatIndexCn(this long index, bool isTraditional = false, int unitCount = 2)
        {
            StringBuilder result = new StringBuilder();
            if (index == 0)
            {
                return isTraditional ? "〇" : "零";
            };
            if (index < 0)
            {
                result.Append("负");
                if (index == long.MinValue) // 处理 long.MinValue 的特殊情况
                    return result.Append("九百二十二万三千三百七十二兆零三百六十八亿五千四百七十七万五千八百零七").ToString();
                index = -index;
            }
            if (index <= 100000L)
            {
                ParseIndexIn10000Cn(index, isTraditional, unitCount, result);
                return result.ToString();
            }

            var compareBase = 1000000000000L;
            var unitBase = 10000L;
            var unitCountTemp = unitCount < 0 ? int.MaxValue : unitCount;

            if (index / compareBase >= unitBase)
            {
                var head = index / compareBase / unitBase;
                var displayCount = ParseIndexIn10000Cn(head, isTraditional, unitCountTemp, result);
                result.Append(GetNumberUnitCn(unitBase, isTraditional));
                unitCountTemp -= displayCount;
                if (unitCountTemp < 1)
                {
                    result.Append("兆");
                    return result.ToString();
                }
                index = index % (compareBase * unitBase);
            }

            while (compareBase > 0L)
            {
                if (index >= compareBase)
                {
                    var head = index / compareBase;
                    var displayCount = ParseIndexIn10000Cn(head, isTraditional, unitCountTemp, result);
                    if (index >= 10000) result.Append(GetNumberUnitCn(index, isTraditional));
                    unitCountTemp -= displayCount;
                    if (unitCountTemp < 1)
                        break;
                }
                index = index % compareBase;
                compareBase /= unitBase;
            }

            return result.ToString();
        }

        private static int ParseIndexIn10000Cn(long index, bool isTraditional, int unitCount, StringBuilder sb)
        {
            var baseValue = 10000L;
            var count = 0;
            // if (sb.Length > 1 && index < 1000L)
            // {
            //     sb.Append(IntToChineseHandler(0, isTraditional)); // 补零
            // }
            while (baseValue > 0L)
            {
                if (index >= baseValue)
                {
                    var headNumber = Mathf.FloorToInt(index * 1f / baseValue);
                    sb.Append(IntToChineseHandler(headNumber, isTraditional));
                    sb.Append(GetNumberUnitCn(index, isTraditional));
                    count++;
                    if (unitCount > 0 && count >= unitCount)
                        break;
                }
                index = index % baseValue;
                baseValue = baseValue / 10L;
                if (index == 0) break;
                if (baseValue >= 10L && index < baseValue && sb.Length > 1)
                {
                    sb.Append(IntToChineseHandler(0, isTraditional)); // 补零
                    while (baseValue >= 10L && index < baseValue)
                    {
                        baseValue = baseValue / 10L;
                    }
                }
            }
            return count;
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

        private static int[] _romanValues = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        private static string[] _romanSymbols = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

        public static string ToRomanNumber(int number)
        {
            if (number <= 0)
            {
                return string.Empty;
            }

            var result = string.Empty;
            for (var i = 0; i < _romanValues.Length; i++)
            {
                while (number >= _romanValues[i])
                {
                    result += _romanSymbols[i];
                    number -= _romanValues[i];
                }
            }

            return result;
        }
    }
}