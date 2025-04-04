using System;
using UnityEngine;

namespace PowerCellStudio
{
    public class CalendarDay
    {
        public int year => _date.Year;
        
        public int month => _date.Month;
        
        public int day => _date.Day;

        private DateTime _date;
        public DateTime date => _date;

        private string _lunarDate;
        public string lunarDate => _lunarDate;

        private string _festival;
        public string festival => _festival;

        public bool isToday => CalendarGenerator.IsToday(_date);

        public CalendarDay(DateTime dateInput)
        {
            _date = dateInput;
            _lunarDate = CalendarGenerator.GetLunarDate(_date, out var lunarFestival);
            _festival = LocalizationManager.instance.isChinese
                ? $"{CalendarGenerator.GetFestival(_date)}{lunarFestival}"
                : CalendarGenerator.GetFestival(_date);
        }

        public string dayOfWeekStr
        {
            get
            {
                var isChinese = LocalizationManager.instance.curLanguage == Language.ChineseSimplified
                                || LocalizationManager.instance.curLanguage == Language.ChineseTraditional;
                var isChineseTraditional = LocalizationManager.instance.curLanguage == Language.ChineseTraditional;
                return TimeUtils.GetWeekDayStr(_date.DayOfWeek, isChinese, isChineseTraditional);
            }
        }
    }
}
