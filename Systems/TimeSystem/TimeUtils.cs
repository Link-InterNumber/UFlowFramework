using System;
using System.Globalization;

namespace PowerCellStudio
{
    public static class TimeUtils
    {
        public static DateTime GetToday()
        {
            return DateTime.Today;
        }
        
        public static long GetTimeStamp(DateTime date)
        {
            return new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }

        public static long GetTimeStamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static DateTime GetTime(long timeStamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;
        }

        public static TimeSpan GetTimeSpan(long timeStampMin, long timeStampMax)
        {
            if (timeStampMin > timeStampMax)
                return TimeSpan.Zero;
            return new TimeSpan((timeStampMax - timeStampMin) * 100);
        }
        
        public static TimeSpan GetTimeSpan(long timeStampDelta)
        {
            return new TimeSpan(timeStampDelta * 100);
        }
        
        public static string FormatTime(this int timeInSec)
        {
            var timeSpan = new TimeSpan(0,0,0,timeInSec);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }
        
        public static string FormatTimeStamp(this long timeStamp)
        {
            var timeSpan = new TimeSpan(0,0,0,0,(int)timeStamp);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }
        
        public static string GetWeekDayStr(int dayOfWeek, bool isChinese, bool isChineseTraditional)
        {
            return (dayOfWeek >= 0 && dayOfWeek < 7) ? GetWeekDayStr((DayOfWeek)dayOfWeek, isChinese, isChineseTraditional) : string.Empty;
        }

        public static string GetWeekDayStr(DayOfWeek dayOfWeek, bool isChinese, bool isChineseTraditional)
        {
            if (isChinese && dayOfWeek != DayOfWeek.Sunday)
            {
                return $"周{NumberDisplay.FormatIndexCn((int)dayOfWeek, isChineseTraditional)}";
            }

            if (isChinese && dayOfWeek == DayOfWeek.Sunday)
            {
                return $"周天";
            }
                
            switch (dayOfWeek)
            {
                case DayOfWeek.Friday:
                    return "Fri.";
                case DayOfWeek.Monday:
                    return "Mon.";
                case DayOfWeek.Saturday:
                    return "Sat.";
                case DayOfWeek.Sunday:
                    return "Sun.";
                case DayOfWeek.Thursday:
                    return "Thurs.";
                case DayOfWeek.Tuesday:
                    return "Tues.";
                case DayOfWeek.Wednesday:
                    return "Wed.";
                default:
                    return string.Empty;
            }
        }
        
        public static string IntToChineseMonth(this int num, bool isTraditional)
        {
            return num switch
            {
                1 => "正月",
                11 => "冬月",
                12 => isTraditional ? "臘月" : "腊月",
                _ => $"{NumberDisplay.FormatIndexCn(num, isTraditional)}月",
            };
        }
        
        /// <summary>
        /// 将月份数字转换为英文月份名称
        /// </summary>
        /// <param name="month">月份数字 (1-12)</param>
        /// <param name="abbreviate">是否返回缩写格式 (默认: 全称)</param>
        /// <returns>英文月份名称</returns>
        /// <exception cref="ArgumentOutOfRangeException">输入无效月份时抛出</exception>
        public static string IntToEnglishMonth(int month, bool abbreviate = false)
        {
            // 验证输入有效性
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month), 
                    "Month must be between 1 and 12");
            }

            // 创建任意年份的日期对象（仅用于获取月份名称）
            DateTime date = new DateTime(2023, month, 1);
        
            // 根据选项选择格式模式
            string formatPattern = abbreviate ? "MMM" : "MMMM";
        
            // 返回不依赖本地化设置的英文月份名称
            return date.ToString(formatPattern, CultureInfo.InvariantCulture);
        }

        public static string IntToChineseDay(this int num, bool isTraditional)
        {
            if (num <= 10)
            {
                return $"初{NumberDisplay.IntToChineseHandler(num, isTraditional)}";
            }
            if (num <= 20)
            {
                return NumberDisplay.FormatIndexCn(num, isTraditional);
            }
            if (num < 30)
            {
                return $"廿{NumberDisplay.IntToChineseHandler(num % 10, isTraditional)}";
            }
            return NumberDisplay.FormatIndexCn(num, isTraditional);
        }
    }
}