using System;
using System.Globalization;

namespace PowerCellStudio
{
    /// <summary>
    /// 时间工具类，包含各种时间处理实用方法。
    /// Time utility class containing various useful methods for handling time.
    /// </summary>
    public static class TimeUtils
    {
        /// <summary>
        /// 获取今天的日期（本地时间，时间为00:00:00）。
        /// Get today's date (local time, time set to 00:00:00).
        /// </summary>
        /// <returns>今天的日期 / Today's date</returns>
        public static DateTime GetToday()
        {
            return DateTime.Today;
        }

        /// <summary>
        /// 获取指定时间的Unix时间戳（毫秒）。
        /// Get Unix timestamp (milliseconds) of a specified date.
        /// </summary>
        /// <param name="date">要转换的时间 / Date to be converted</param>
        /// <returns>Unix时间戳（毫秒） / Unix timestamp (milliseconds)</returns>
        public static long GetTimeStamp(DateTime date)
        {
            return new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 获取当前UTC时间的Unix时间戳（毫秒）。
        /// Get the current Unix timestamp (milliseconds) in UTC.
        /// </summary>
        /// <returns>Unix时间戳（毫秒） / Unix timestamp (milliseconds)</returns>
        public static long GetTimeStamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 将Unix时间戳（毫秒）转换为本地时间。
        /// Convert Unix timestamp (milliseconds) to local time.
        /// </summary>
        /// <param name="timeStamp">Unix时间戳（毫秒） / Unix timestamp (milliseconds)</param>
        /// <returns>本地时间 / Local time</returns>
        public static DateTime GetTime(long timeStamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;
        }

        /// <summary>
        /// 计算两个Unix时间戳（毫秒）之间的时间间隔。
        /// Calculate the time span between two Unix timestamps (milliseconds).
        /// </summary>
        /// <param name="timeStampMin">较小的时间戳（毫秒） / Smaller timestamp (milliseconds)</param>
        /// <param name="timeStampMax">较大的时间戳（毫秒） / Larger timestamp (milliseconds)</param>
        /// <returns>时间间隔 / Time span</returns>
        public static TimeSpan GetTimeSpan(long timeStampMin, long timeStampMax)
        {
            if (timeStampMin > timeStampMax)
                return TimeSpan.Zero;
            return new TimeSpan((timeStampMax - timeStampMin) * TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// 根据时间戳差值（毫秒）获取时间间隔。
        /// Get time span based on timestamp difference (milliseconds).
        /// </summary>
        /// <param name="timeStampDelta">时间戳差值（毫秒） / Timestamp difference (milliseconds)</param>
        /// <returns>时间间隔 / Time span</returns>
        public static TimeSpan GetTimeSpan(long timeStampDelta)
        {
            return new TimeSpan(timeStampDelta * TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// 将秒数格式化为 hh:mm:ss 字符串。
        /// Format seconds into a hh:mm:ss string representation.
        /// </summary>
        /// <param name="timeInSec">秒数 / Time in seconds</param>
        /// <returns>格式化字符串 / Formatted string</returns>
        public static string FormatTime(this int timeInSec)
        {
            var timeSpan = new TimeSpan(0, 0, 0, timeInSec);
            return FormatTime(timeSpan);
        }
        
        public static string FormatTime(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 100)
            {
                return $"{(int)timeSpan.TotalHours}:{timeSpan:mm\\:ss}";
            }
            if (timeSpan.TotalMinutes >= 60)
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
            return timeSpan.ToString(@"mm\:ss");
        }

        /// <summary>
        /// 将毫秒时间戳格式化为 hh:mm:ss 字符串。
        /// Format milliseconds timestamp into a hh:mm:ss string representation.
        /// </summary>
        /// <param name="timeStamp">毫秒时间戳 / Milliseconds timestamp</param>
        /// <returns>格式化字符串 / Formatted string</returns>
        public static string FormatTimeStamp(this long timeStamp)
        {
            var timeSpan = new TimeSpan(timeStamp * TimeSpan.TicksPerMillisecond);
            return timeSpan.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// 获取星期几的字符串表示。
        /// Get string representation of the day of the week.
        /// </summary>
        /// <param name="dayOfWeek">星期几（0-6） / Day of the week (0-6)</param>
        /// <param name="isChinese">是否中文 / Whether to use Chinese</param>
        /// <param name="isChineseTraditional">是否繁体 / Whether to use traditional Chinese</param>
        /// <returns>星期字符串 / Weekday string</returns>
        public static string GetWeekDayStr(int dayOfWeek, bool isChinese, bool isChineseTraditional)
        {
            return (dayOfWeek >= 0 && dayOfWeek < 7) ? GetWeekDayStr((DayOfWeek)dayOfWeek, isChinese, isChineseTraditional) : string.Empty;
        }

        /// <summary>
        /// 获取星期几的字符串表示。
        /// Get string representation of the day of the week.
        /// </summary>
        /// <param name="dayOfWeek">DayOfWeek 枚举 / DayOfWeek enumeration</param>
        /// <param name="isChinese">是否中文 / Whether to use Chinese</param>
        /// <param name="isChineseTraditional">是否繁体 / Whether to use traditional Chinese</param>
        /// <returns>星期字符串 / Weekday string</returns>
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

        /// <summary>
        /// 将月份数字转换为中文月份名称。
        /// Convert month number to Chinese month name.
        /// </summary>
        /// <param name="num">月份数字 / Month number</param>
        /// <param name="isTraditional">是否繁体 / Whether to use traditional Chinese</param>
        /// <returns>中文月份 / Chinese month name</returns>
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
        /// 将月份数字转换为英文月份名称。
        /// Convert month number to English month name.
        /// </summary>
        /// <param name="month">月份数字 (1-12) / Month number (1-12)</param>
        /// <param name="abbreviate">是否返回缩写格式 (默认: 全称) / Whether to return abbreviated format (default: full name)</param>
        /// <returns>英文月份名称 / English month name</returns>
        /// <exception cref="ArgumentOutOfRangeException">输入无效月份时抛出 / Throws when an invalid month is input</exception>
        public static string IntToEnglishMonth(int month, bool abbreviate = false)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month must be between 1 and 12");
            }

            DateTime date = new DateTime(2023, month, 1);
            string formatPattern = abbreviate ? "MMM" : "MMMM";
            return date.ToString(formatPattern, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 将日期数字转换为中文日期名称。
        /// Convert day number to Chinese day name.
        /// </summary>
        /// <param name="num">日期数字 / Day number</param>
        /// <param name="isTraditional">是否繁体 / Whether to use traditional Chinese</param>
        /// <returns>中文日期 / Chinese day name</returns>
        public static string IntToChineseDay(this int num, bool isTraditional)
        {
            if (num <= 10)
            {
                return $"初{NumberDisplay.IntToChineseHandler(num, isTraditional)}";
            }
            if (num < 20)
            {
                return $"十{NumberDisplay.IntToChineseHandler(num - 10, isTraditional)}";
            }
            if (num == 20)
            {
                return "廿十";
            }
            if (num < 30)
            {
                return $"廿{NumberDisplay.IntToChineseHandler(num - 20, isTraditional)}";
            }
            return NumberDisplay.FormatIndexCn(num, isTraditional);
        }

        /// <summary>
        /// 将日期数字转换为英文日期名称。
        /// Convert day number to English day name.
        /// </summary>
        /// <param name="num">日期数字 (1-31) / Day number (1-31)</param>
        /// <param name="abbreviate">是否返回缩写格式 (默认: 两位) / Whether to return abbreviated format (default: two digits)</param>
        /// <returns>英文日期名称 / English day name</returns>
        /// <exception cref="ArgumentOutOfRangeException">输入无效日期时抛出 / Throws when an invalid day is input</exception>
        public static string IntToEnglishDay(int num, bool abbreviate = false)
        {
            if (num < 1 || num > 31)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(num),
                    "Day must be between 1 and 31");
            }

            DateTime date = new DateTime(2023, 1, num);
            string formatPattern = abbreviate ? "d" : "dd";
            return date.ToString(formatPattern, CultureInfo.InvariantCulture);
        }
    }
}