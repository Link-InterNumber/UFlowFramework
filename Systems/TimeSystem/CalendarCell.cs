using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class CalendarCell : ListItem
    {
        public Text dateText;
        public Text lunarText;
        public Text festivalText;
        public Image background;
        public Button btn;

        private CalendarDay _calendarDay;
        public CalendarDay calendarDay => _calendarDay;
        
        public struct PassData
        {
            public CalendarDay day;
            public CalendarGenerator generator;

            public PassData(CalendarDay calendarDay, CalendarGenerator generator)
            {
                day = calendarDay;
                this.generator = generator;
            }
        }

        private void Awake()
        {
            btn.onClick.AddListener(OnClickCell);
        }

        private void OnDestroy()
        {
            btn.onClick.RemoveListener(OnClickCell);
        }

        private void OnClickCell()
        {
            itemHolder.ItemInteraction(this, null);
        }

        public override void UpdateContent(int index, object data, IListUpdater holder)
        {
            base.UpdateContent(index, data, holder);
            var passData = (PassData) data;
            Init(passData.day, passData.generator);
        }

        public void Init(CalendarDay calendarDay, CalendarGenerator generator)
        {
            _calendarDay = calendarDay;
            SetDate(calendarDay.day,
                generator.currentDisplayMonth == calendarDay.month, 
                calendarDay.isToday);
            SetLunarText(calendarDay.lunarDate);
            var festivals = calendarDay.festival.Replace('|', ' ');
            SetFestivalText(festivals);
        }

        private static Color _halfClear = new Color(0.8f, 0.8f, 0.8f, 1f);
        private void SetDate(int day, bool isCurrentMonth, bool isToday)
        {
            dateText.text = day.ToString();
            dateText.color = isCurrentMonth ? Color.black : Color.gray;
            if (isToday)
            {
                background.color = new Color(0.8f, 0.9f, 1f);
            }
            else if(isCurrentMonth)
            {
                background.color = Color.white;
            }
            else
            {
                background.color = _halfClear;
            }
        }

        private void SetLunarText(string text)
        {
            lunarText.text = LocalizationManager.instance.isChinese ? text : string.Empty;
        }

        private void SetFestivalText(string text)
        {
            festivalText.text = text;
        }
    }
}