using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PowerCellStudio
{
    public class CalendarGenerator 
    {
        public enum StartDayOfWeek
        {
            Sunday,
            Monday,
        }
        
        public static StartDayOfWeek startDayOfWeek = StartDayOfWeek.Monday;
        
        private DateTime _currentDate;
        public DateTime currentDate => _currentDate;
        
        private static ChineseLunisolarCalendar _chineseCalendar = new ChineseLunisolarCalendar();
        
        // 简单节假日数据（可扩展）
        // TODO 配置为配置表
        private static Dictionary<int, string[]> _festivals = new Dictionary<int, string[]>()
        {
            { 0101, new []{"元旦|", "元旦|"} },
            { 0214, new []{"情人节|", "情人節|"} },
            { 0308, new []{"妇女节|", "婦女節|"}},
            { 0312, new []{"植树节|", "植樹節|"}},
            { 0401, new []{"愚人节|", "愚人節|"}},
            { 0501, new []{"劳动节|", "勞動節|"} },
            { 0601, new []{"儿童节|", "兒童節|"} },
            { 1001, new []{"国庆节|", "國慶節|"} },
            { 1225, new []{"圣诞节|", "聖誕節|"} }
        };

        private static Dictionary<int, string[]> _lunarFestivals = new Dictionary<int, string[]>()
        {
            { 0101, new []{"春节|", "春節|"}},
            { 0115, new []{"元宵节|", "元宵節|"}},
            { 0202, new []{"龙抬头|", "龍抬頭|"}},
            { 0307, new []{"清明节|", "清明節|"}},
            { 0505, new []{"端午节|", "端午節|"}},
            { 0707, new []{"七夕节|", "七夕節|"}},
            { 0815, new []{"中秋节|", "中秋節|"}},
            { 0909, new []{"重阳节|", "重陽節|"}},
            { 1208, new []{"腊八节|", "臘八節|"}},
            { 1230, new []{"除夕|", "除夕|"}},
        };

        private static Dictionary<int, string[]> _solarTerms = new Dictionary<int, string[]>()
        {
            { 0204, new []{"立春", "立春"} },
            { 0219, new []{"雨水", "雨水"} },
            { 0306, new []{"惊蛰", "驚蟄"} },
            { 0321, new []{"春分", "春分"} },
            { 0405, new []{"清明", "清明"} },
            { 0420, new []{"谷雨", "穀雨"} },
            { 0506, new []{"立夏", "立夏"} },
            { 0521, new []{"小满", "小滿"} },
            { 0606, new []{"芒种", "芒種"} },
            { 0621, new []{"夏至", "夏至"} },
            { 0707, new []{"小暑", "小暑"} },
            { 0723, new []{"大暑", "大暑"} },
            { 0808, new []{"立秋", "立秋"} },
            { 0823, new []{"处暑", "處暑"} },
            { 0908, new []{"白露", "白露"} },
            { 0923, new []{"秋分", "秋分"} },
            { 1008, new []{"寒露", "寒露"} },
            { 1023, new []{"霜降", "霜降"} },
            { 1107, new []{"立冬", "立冬"} },
            { 1122, new []{"小雪", "小雪"} },
            { 1207, new []{"大雪", "大雪"} },
            { 1222, new []{"冬至", "冬至"} },
            { 0105, new []{"小寒", "小寒"} },
            { 0120, new []{"大寒", "大寒"} },
        };

        public CalendarGenerator()
        {
            _currentDate = DateTime.Now;
        }

        public CalendarDay[] GenerateCalendar(DateTime date)
        {
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            // int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            int startDay = startDayOfWeek == StartDayOfWeek.Sunday
                ? (int)firstDayOfMonth.DayOfWeek
                : ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            // 填充前一个月的天数
            // DateTime prevMonth = date.AddMonths(-1);
            // var nextMonth = date.AddMonths(1);
            // int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);

            var result = new CalendarDay[42];
            // 创建日期格子
            for (int i = 0; i < 42; i++) // 6行×7列
            {
                int dayNumber = i - startDay - date.Day + 1;
                var currentDay = date.AddDays(dayNumber);
                // var monthNumber = date.Month;
                // if (i < startDay)
                // {
                //     // 上个月日期
                //     dayNumber = daysInPrevMonth - startDay + i + 1;
                //     monthNumber = prevMonth.Month;
                // }
                // else if (i >= daysInMonth + startDay)
                // {
                //     // 下个月日期
                //     dayNumber = i - daysInMonth - startDay + 1;
                //     monthNumber = nextMonth.Month;
                // }
                // else
                // {
                //     // 当月日期
                //     dayNumber = i - startDay + 1;
                // }
                // DateTime currentDay = new DateTime(date.Year, monthNumber, dayNumber);
                var day = new CalendarDay(currentDay);
                result[i] = day;
            }
            return result;
        }

        public static bool IsToday(DateTime date)
        {
            return IsToday(date.Year, date.Month, date.Day);
        }

        private static bool IsToday(int year, int month, int day)
        {
            return year == DateTime.Now.Year &&
                   month == DateTime.Now.Month &&
                   day == DateTime.Now.Day;
        }

        public static string GetLunarDate(DateTime date, out string lunarFestival)
        {
            try
            {
                if (_chineseCalendar.MaxSupportedDateTime < date ||
                    _chineseCalendar.MinSupportedDateTime > date)
                {
                    lunarFestival = string.Empty;
                    return string.Empty;
                }
                CalLunarMonthNDay(date, out var lunarMonth, out var lunarDay, out var isLeapMonth);
                var isTran = LocalizationManager.instance.curLanguage == Language.ChineseTraditional;
                
                var key = lunarMonth * 100 + lunarDay;
                if (_lunarFestivals.ContainsKey(key))
                    lunarFestival = _lunarFestivals[key][isTran?1:0];
                else if (_solarTerms.ContainsKey(key))
                    lunarFestival = _solarTerms[key][isTran?1:0];
                else 
                    lunarFestival = string.Empty;
                
                return $"{(isLeapMonth ? "闰" : "")}{lunarMonth.IntToChineseMonth(isTran)} {lunarDay.IntToChineseDay(isTran)}";
            }
            catch
            {
                lunarFestival = string.Empty;
                return "";
            }
        }

        private static void CalLunarMonthNDay(DateTime date,
            out int lunarMonth, out int lunarDay, out bool isLeapMonth)
        {
            lunarMonth = _chineseCalendar.GetMonth(date);
            lunarDay = _chineseCalendar.GetDayOfMonth(date);
            isLeapMonth = false;
            int leapMonth = _chineseCalendar.GetLeapMonth(date.Year);
            if (leapMonth > 0)
            {
                isLeapMonth = leapMonth == lunarMonth;
                if (leapMonth <= lunarMonth) lunarMonth--;
                return;
            }
            
            var lastYearLeapMonth = _chineseCalendar.GetLeapMonth(date.Year - 1);
            if (lastYearLeapMonth > 0)
            {
                var lunarYear = _chineseCalendar.GetSexagenaryYear(date);
                var lastLunarYear = _chineseCalendar.GetSexagenaryYear(new DateTime(date.Year -1, 12, 31));
                // 判断是在农历去年的范围内
                if (lunarYear == lastLunarYear)
                {
                    isLeapMonth = lastYearLeapMonth == lunarMonth;
                    if (lastYearLeapMonth <= lunarMonth) lunarMonth--;
                }
            }
        }

        public static string GetFestival(DateTime date, bool concatLunarFestival = false)
        {
            var isTran = LocalizationManager.instance.curLanguage == Language.ChineseTraditional;
            var sb = new StringBuilder();
            // 阳历节日
            var key = date.Month * 100 + date.Day;
            if (_festivals.ContainsKey(key))
                sb.Append(_festivals[key][isTran?1:0]);
            if (concatLunarFestival) 
                sb.Append(GetLunarFestival(date));
            // 这里可以添加农历节日判断
            return sb.ToString();
        }

        private static string GetLunarFestival(DateTime date)
        {
            try
            {
                if (_chineseCalendar.MaxSupportedDateTime < date ||
                    _chineseCalendar.MinSupportedDateTime > date)
                    return string.Empty;
                CalLunarMonthNDay(date, out var lunarMonth, out var lunarDay, out _);
                var isTran = LocalizationManager.instance.curLanguage == Language.ChineseTraditional;
                var key = lunarMonth * 100 + lunarDay;
                if (_lunarFestivals.ContainsKey(key))
                {
                    return _lunarFestivals[key][isTran?1:0];
                }
                if (_solarTerms.ContainsKey(key))
                {
                    return _solarTerms[key][isTran?1:0];
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public void ChangeMonth(int direction)
        {
            _currentDate = _currentDate.AddMonths(direction);
        }

        public void Reset()
        {
            _currentDate = DateTime.Now;
        }

        public int currentDisplayMonth => _currentDate.Month;
        public int currentDisplayYear => _currentDate.Year;
    }
}