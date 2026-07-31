using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(Text))]
    public class SliderNumDisplay : MonoBehaviour
    {
        public Slider SliderNode;
        public bool DisplayInPercent;
        public bool DisplayDenominator = false;
        public string Format = "N0";
        private Text _text;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void OnEnable()
        {
            if(!SliderNode) return;
            OnSliderValueChange(SliderNode.value);
            SliderNode.onValueChanged.AddListener(OnSliderValueChange);
        }
        
        private void OnDisable()
        {
            if(!SliderNode) return;
            SliderNode.onValueChanged.RemoveListener(OnSliderValueChange);
        }

        private void OnSliderValueChange(float arg0)
        {
            var result = DisplayInPercent ? $"{(arg0 * 100).ToString(Format)}%" : arg0.ToString(Format);
            if (DisplayDenominator)
            {
                result += DisplayInPercent? "100%" : $"/{SliderNode.maxValue}";
            }
            _text.text = result;
        }
    }
}