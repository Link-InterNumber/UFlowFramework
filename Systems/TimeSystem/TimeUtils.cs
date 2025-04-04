using System;

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