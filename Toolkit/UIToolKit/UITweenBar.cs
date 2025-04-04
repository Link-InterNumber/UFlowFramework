using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class UITweenBar : MonoBehaviour
    {
        public Slider TrackBar;
        public Slider AppearBar;
        public float TweenDuration = 0.5f;

        private float _currentValue = 0;

        public float BarValue
        {
            set => SetValue(value);
            get => _currentValue;
        }

        public bool IsShow => gameObject.activeSelf;

        public static float operator +(UITweenBar x, float y)
        {
            x.SetDelta(y);
            return x.BarValue;
        }
        
        public static float operator -(UITweenBar x, float y)
        {
            x.SetDelta(-y);
            return x.BarValue;
        }

        private void OnEnable()
        {
            AppearBar.value = _currentValue;
            if(TrackBar) TrackBar.value = _currentValue;
        }

        public void HideBar()
        {
            gameObject.SetActive(false);
        }
        
        public void ShowBar()
        {
            gameObject.SetActive(true);
        }

        public void ResetBar(float value = 0)
        {
            value = Mathf.Clamp01(value);
            SetTrackBarValue(value);
            AppearBar.value = value;
            _currentValue = value;
        }

        private Coroutine _coroutineAppearBar;
        private void DoAppearBar(float val, float time, Action call)
        {
            if(_coroutineAppearBar != null) ApplicationManager.instance.StopCoroutine(_coroutineAppearBar);
            _coroutineAppearBar = ApplicationManager.instance.StartCoroutine(DoValue(AppearBar, val, time, call));
        }
        
        private Coroutine _coroutineTrackBar;
        private void DoTrackBar(float val, float time)
        {
            if(_coroutineTrackBar != null) ApplicationManager.instance.StopCoroutine(_coroutineTrackBar);
            _coroutineTrackBar = ApplicationManager.instance.StartCoroutine(DoValue(TrackBar, val, time));
        }

        private IEnumerator DoValue(Slider slider, float val, float time, Action call = null)
        {
            if(!slider) yield break;
            var startValue = slider.value;
            var timePass = 0f;
            while (timePass < time)
            {
                var normalized = Ease.GetEase(EaseType.EaseOutSine, Mathf.Clamp01(timePass / time));
                slider.value = Mathf.Lerp(startValue, val, normalized);
                timePass += Time.unscaledTime;
                if(!slider) yield break;
                yield return null;
            }
            if (slider) slider.value = val;
            call?.Invoke();
        }

        public void ShowUp()
        {
            _currentValue = 1;
            SetTrackBarValue(0);
            AppearBar.value = 0;
            DoAppearBar(1f, 2f, () => SetTrackBarValue(1));
            SetTrackBarValue(1);
        }

        public void SetDelta(float deltaValue)
        {
            var tempValue = AppearBar.value + deltaValue;
            tempValue = Mathf.Clamp01(tempValue);
            SetValue(tempValue);
        }

        public void SetValue(int curValue, int totalValue, bool playAni = true)
        {
            SetValue(curValue * 1.0f / totalValue, playAni);
        }

        public void SetValue(float inputValue, bool playAni = true)
        {
            inputValue = Mathf.Clamp01(inputValue);
            if(Mathf.Approximately(inputValue,_currentValue))
                return;
            if (!playAni)
            {
                _currentValue = inputValue;
                AppearBar.value = inputValue;
                SetTrackBarValue(inputValue);
                return;
            }
            if (inputValue > _currentValue)
            {
                AddValue(inputValue);
            }
            else
            {
                SubValue(inputValue);
            }
            _currentValue = inputValue;
        }

        private void AddValue(float value)
        {
            DoAppearBar(value, TweenDuration, () => SetTrackBarValue(value));
        }

        private void SubValue(float value)
        {
            gameObject.SetActive(true);
            // m_trackBar.value = m_previousValue;
            AppearBar.value = value;
            // await Task.Delay(1000);
            if(TrackBar)
                DoTrackBar(value, TweenDuration);
        }

        private void SetTrackBarValue(float value)
        {
            if(TrackBar)
                TrackBar.value = value;
        }
    }
}