using System.Collections;
using System.Linq;
using Opoop;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class CalendarTable : MonoBehaviour
    {
        public ListUpdater listDay;
        public Text monthYearText;
        public Text[] dayOfWeek = new Text[7];
        public Button btnLastMonth;
        public Button btnNextMonth;

        private CalendarGenerator _calendarGenerator;
        public LinkEvent onChangeMonth = new LinkEvent();
        
        private void Awake()
        {
            btnLastMonth.onClick.AddListener(OnClickLastMonth);
            btnNextMonth.onClick.AddListener(OnClickNextMonth);
            EventManager.instance.onLanguageChange.AddListener(OnLanguageChange);
        }

        private void OnDestroy()
        {
            btnLastMonth.onClick.RemoveListener(OnClickLastMonth);
            btnNextMonth.onClick.RemoveListener(OnClickNextMonth);
            EventManager.instance.onLanguageChange.RemoveListener(OnLanguageChange);
            _calendarGenerator = null;
        }

        private void OnLanguageChange(Language data)
        {
            if (dayOfWeek != null && dayOfWeek.Length == 7)
            {
                UpdateWeekOfDay();
            }
            UpdateTable(_calendarGenerator);
            onChangeMonth.Invoke();
        }

        public void Init(CalendarGenerator generator)
        {
            if (generator == null) return;
            if (dayOfWeek != null && dayOfWeek.Length == 7)
            {
                UpdateWeekOfDay();
            }
            _calendarGenerator = generator;
            UpdateTable(_calendarGenerator);
            onChangeMonth.Invoke();
        }

        private void OnClickLastMonth()
        {
            if (_calendarGenerator == null || _changeMonthCoroutine != null) return;
            _calendarGenerator.ChangeMonth(-1);
            UpdateTable(_calendarGenerator);
            onChangeMonth.Invoke();
            // _changeMonthCoroutine = ApplicationManager.instance.StartCoroutine(OnChangeMonthHandler());
        }

        private void OnClickNextMonth()
        {
            if (_calendarGenerator == null || _changeMonthCoroutine != null) return;
            _calendarGenerator.ChangeMonth(1);
            UpdateTable(_calendarGenerator);
            onChangeMonth.Invoke();
            // _changeMonthCoroutine = ApplicationManager.instance.StartCoroutine(OnChangeMonthHandler());
        }

        // private Coroutine _changeMonthCoroutine;
        // private IEnumerator OnChangeMonthHandler()
        // {
        //     UpdateTable(_calendarGenerator);
        //     yield return OpoopManager.instance.ResetOpoopSaveData(_calendarGenerator.currentDate);
        //     yield return OpoopManager.instance.ResetMoodSaveData(_calendarGenerator.currentDate);
        //     onChangeMonth.Invoke();
        //     _changeMonthCoroutine = null;
        // }

        private void UpdateWeekOfDay()
        {
            var starWithSunday = CalendarGenerator.startDayOfWeek == CalendarGenerator.StartDayOfWeek.Sunday;
            for (int i = 0; i < 7; i++)
            {
                if (!dayOfWeek[i]) continue;
                var weekIndex = starWithSunday ? i : ((i + 1) % 7);
                var isChinese = LocalizationManager.instance.isChinese;
                var isChineseTraditional = LocalizationManager.instance.isChineseTraditional;
                dayOfWeek[i].text = TimeUtils.GetWeekDayStr(weekIndex, isChinese, isChineseTraditional);
            }
        }

        public void UpdateTable(CalendarGenerator generator)
        {
            if (generator == null) return;
            UpdateMonthYearText(generator);
            var days = generator.GenerateCalendar(generator.currentDate);
            var dataList = ListPool<CalendarCell.PassData>.Get();
            dataList.AddRange(days.Select(o=> new CalendarCell.PassData(o, generator)));
            listDay.UpdateList(dataList);
            ListPool<CalendarCell.PassData>.Release(dataList);
        }

        private void UpdateMonthYearText(CalendarGenerator generator)
        {
            monthYearText.text = LocalizationManager.instance.isChinese
                ? $"{generator.currentDisplayYear}年{generator.currentDisplayMonth}月"
                : $"{TimeUtils.IntToEnglishMonth(generator.currentDisplayMonth)} {generator.currentDisplayYear}";
        }
        
        private void ClearCalendar()
        {
            listDay.Clear();
        }
    }
}