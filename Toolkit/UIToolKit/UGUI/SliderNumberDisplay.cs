using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Text))]
    public class SliderNumDisplay : MonoBehaviour
    {
        public Slider sliderNode;
        public bool displayInPercent;
        public bool displayDenominator = false;
        public string format = "N0";
        private Text _text;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void OnEnable()
        {
            if(!sliderNode) return;
            OnSliderValueChange(sliderNode.value);
            sliderNode.onValueChanged.AddListener(OnSliderValueChange);
        }
        
        private void OnDisable()
        {
            if(!sliderNode) return;
            sliderNode.onValueChanged.RemoveListener(OnSliderValueChange);
        }

        private void OnSliderValueChange(float arg0)
        {
            var result = displayInPercent ? $"{(arg0 * 100).ToString(format)}%" : arg0.ToString(format);
            if (displayDenominator)
            {
                result += displayInPercent? "100%" : $"/{sliderNode.maxValue}";
            }
            _text.text = result;
        }
    }
}