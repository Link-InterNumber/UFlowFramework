using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class UITweenBar : MonoBehaviour
    {
        public Slider trackBar;
        public Slider appearBar;
        public float tweenDuration = 0.5f;

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

        private void Start()
        {
            if (appearBar)
            {
                appearBar.minValue = 0f;
                appearBar.maxValue = 1f;
                appearBar.wholeNumbers = false;
            }
            if (trackBar)
            {
                trackBar.minValue = 0f;
                trackBar.maxValue = 1f;
                trackBar.wholeNumbers = false;
            }
        }

        private void OnEnable()
        {
            ResetBar(_currentValue);
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
            SetAppearBarValue(value);
            _currentValue = value;
        }

        private class BarTweenUpdater
        {
            public Slider sliderBar;
            public float appearBarTweenTime;
            public float appearBarTweenTimePass;
            public float appearBarStartValue;
            public float appearBarEndValue;

            public bool isDone => !sliderBar || appearBarTweenTimePass >= appearBarTweenTime;

            public void Update(float deltaTime)
            {
                if (!sliderBar) return;
                var normalized = Ease.GetEase(EaseType.OutSine, Mathf.Clamp01(appearBarTweenTimePass / appearBarTweenTime));
                sliderBar.value = Mathf.Lerp(appearBarStartValue, appearBarEndValue, normalized);
                appearBarTweenTimePass += deltaTime;
                if (appearBarTweenTimePass < appearBarTweenTime) return;
                sliderBar.value = appearBarEndValue;
            }
        }

        private BarTweenUpdater _barTweenUpdater;
        private BarTweenUpdater _trackBarTweenUpdater;

        private void DoAppearBar(float val, float time)
        {
            if (_barTweenUpdater == null)
                _barTweenUpdater = new BarTweenUpdater();
            _barTweenUpdater.sliderBar = appearBar;
            _barTweenUpdater.appearBarTweenTime = time;
            _barTweenUpdater.appearBarTweenTimePass = 0;
            _barTweenUpdater.appearBarStartValue = appearBar.value;
            _barTweenUpdater.appearBarEndValue = val;
        }

        private void DoTrackBar(float val, float time)
        {
            if (_trackBarTweenUpdater == null)
                _trackBarTweenUpdater = new BarTweenUpdater();
            _trackBarTweenUpdater.sliderBar = trackBar;
            _trackBarTweenUpdater.appearBarTweenTime = time;
            _trackBarTweenUpdater.appearBarTweenTimePass = 0;
            _trackBarTweenUpdater.appearBarStartValue = trackBar.value;
            _trackBarTweenUpdater.appearBarEndValue = val;
        }

        private void Update()
        {
            if (_barTweenUpdater != null)
            {
                _barTweenUpdater.Update(Time.deltaTime);
                if (_barTweenUpdater.isDone)
                {
                    SetTrackBarValue(appearBar.value);
                    if (trackBar) trackBar.value = appearBar.value;
                    _barTweenUpdater = null;
                }
            }

            if (_trackBarTweenUpdater != null)
            {
                _trackBarTweenUpdater.Update(Time.deltaTime);
                if (_trackBarTweenUpdater.isDone)
                {
                    _trackBarTweenUpdater = null;
                }
            }
        }

        public void ShowUp()
        {
            _currentValue = 1;
            SetTrackBarValue(0);
            appearBar.value = 0;
            DoAppearBar(1f, 2f);
        }

        public void SetDelta(float deltaValue)
        {
            var tempValue = appearBar.value + deltaValue;
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
            if (Mathf.Approximately(inputValue, _currentValue))
                return;
            if (!playAni)
            {
                _currentValue = inputValue;
                SetAppearBarValue(inputValue);
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
            DoAppearBar(value, tweenDuration);
        }

        private void SubValue(float value)
        {
            gameObject.SetActive(true);
            // m_trackBar.value = m_previousValue;
            SetAppearBarValue(value);
            // await Task.Delay(1000);
            if (trackBar)
                DoTrackBar(value, tweenDuration);
        }

        private void SetAppearBarValue(float value)
        {
            _barTweenUpdater = null;
            appearBar.value = value;
        }

        private void SetTrackBarValue(float value)
        {
            _trackBarTweenUpdater = null;
            if (trackBar)
                trackBar.value = value;
        }
    }
}