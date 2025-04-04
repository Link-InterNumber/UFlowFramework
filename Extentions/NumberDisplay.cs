using System;
using System.Text;
using UnityEngine;

namespace PowerCellStudio
{
    public static class NumberDisplay
    {
        public static string FormatNumberEnSign(this int num, int size = 2)
        {
            return num < 0 
                ? "-" + FormatNumberEn(Mathf.Abs(num), size) 
                : FormatNumberEn(Mathf.Abs(num), size);
        }
        
        public static double MillionToRate(this int num)
        {
            return num * 0.0001D;
        }

        public static string FormatNumberEn(this long num, int size = 2)
        {
            var result = new StringBuilder();
            // if( hasSign) result.Append(num > 0? "+": "-");
            if (num > 1000000000L)
            {
                result.Append((num * 0.000000001f).ToString($"N{size}") + "B");
                return result.ToString();
            }
            if (num > 1000000L)
            {
                result.Append((num * 0.000001f).ToString($"N{size}") + "M");
                return result.ToString();
            }
            if (num > 10000L)
            {
                result.Append((num * 0.001f).ToString($"N{size}") + "K");
                return result.ToString();
            }
            return result.Append(num.ToString("N0")).ToString();
        }
        
        public static string FormatNumberCnSign(this int num, int size = 2, bool isTraditional = false)
        {
            return num < 0
                ? "-" + FormatNumberCn(Mathf.Abs(num), size, isTraditional)
                : FormatNumberCn(Mathf.Abs(num), size, isTraditional);
        }
        
        public static string FormatNumberCn(this long num, int size = 2, bool isTraditional = false)
        {
            var result = new StringBuilder();
            if (num > 100000000)
            {
                result.Append((num * 0.00000001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
            }
            if (num > 10000)
            {
                result.Append((num * 0.0001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
            }
            if (num > 1000)
            {
                result.Append((num * 0.001f).ToString($"N{Math.Max(0, size)}"));
                result.Append(GetNumberUnitCn(num, isTraditional));
            }
            return result.ToString();
        }

        public static string GetNumberUnitCn(this long number, bool isTraditional)
        {
            if (number > 100000000)
            {
                return "亿";
            }
            if (number > 10000)
            {
                return "万";
            }
            if (number > 1000)
            {
                return isTraditional ? "仟" : "千";
            }
            if (number > 100)
            {
                return isTraditional ? "佰" : "百";
            }
            if (number > 10)
            {
                return isTraditional ? "拾" : "十";
            }
            return string.Empty;
        }

        public static string FormatNumber(this long num, bool isChinese, bool isTraditional = false, int size = 2)
        {
            return !isChinese ? FormatNumberCn(num, size, isTraditional) : num.FormatNumberEn(size);
        }
        
        public static string FormatIndexCn(this long index, bool isTraditional = false)
        {
            StringBuilder result = new StringBuilder();
            if (index > 100000000)
            {
                result.Append(IntToChineseHandler(Mathf.FloorToInt(index * 0.00000001f), isTraditional));
                result.Append(GetNumberUnitCn(index, isTraditional));
                index %= 100000000;
            }
            if (index > 10000)
            {
                result.Append(IntToChineseHandler(Mathf.FloorToInt(index * 0.0001f), isTraditional));
                result.Append(GetNumberUnitCn(index, isTraditional));
                index %= 10000;
            }
            if (index > 1000)
            {
                result.Append($"{IntToChineseHandler(Mathf.FloorToInt(index * 0.001f), isTraditional)}");
                result.Append(GetNumberUnitCn(index, isTraditional));
                index %= 1000;
            }
            if (index > 100)
            {
                result.Append($"{IntToChineseHandler(Mathf.FloorToInt(index * 0.01f), isTraditional)}");
                result.Append(GetNumberUnitCn(index, isTraditional));
                index %= 100;
            }
            if (index > 10)
            {
                if (index > 19)
                {
                    result.Append($"{IntToChineseHandler(Mathf.FloorToInt(index * 0.1f), isTraditional)}");
                    result.Append(GetNumberUnitCn(index, isTraditional));
                }
                else
                {
                    result.Append($"{(isTraditional ? "拾" : "十")}");
                }
                index %= 10;
            }
            if(index > 0)
                result.Append(IntToChineseHandler(index, isTraditional));
            return result.ToString();
        }

        /// <summary>
        /// 只处理0~10的数字，替换为FormatIndexCn可以处理更大范围内的数字
        /// </summary>
        /// <param name="num">0~10</param>
        /// <param name="isTraditional">是否繁体</param>
        /// <returns></returns>
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
                _ => "",
            };
        }
    }
}