#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class TestAttributeInspector : Editor
    {
        private List<MethodInfo> _buttonMethods;
        private List<MethodInfo> _sliderMethods;
        private Dictionary<MethodInfo, float> _sliderMethodsParams;

        private void OnEnable()
        {
            _buttonMethods = new List<MethodInfo>();
            _sliderMethods = new List<MethodInfo>();
            _sliderMethodsParams = new Dictionary<MethodInfo, float>();
            var type = target.GetType();
            var methods =  type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var attrTestButton = method.GetCustomAttribute<TestButtonAttribute>();
                if (attrTestButton != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                        _buttonMethods.Add(method);
                }
                
                var attrTestSlider = method.GetCustomAttribute<TestSliderAttribute>();
                if (attrTestSlider != null)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
                    {
                        _sliderMethods.Add(method);
                        _sliderMethodsParams.Add(method, attrTestSlider.DefaultValue);
                    }
                }
            }
        }
        
        private void OnDisable()
        {
            _buttonMethods?.Clear();
            _sliderMethods?.Clear();
            _sliderMethodsParams?.Clear();
            _buttonMethods = null;
            _sliderMethods = null;
            _sliderMethodsParams = null;
        }

        public override void OnInspectorGUI()
        {
            // Debug.Log($"target:{this.target}");
            base.OnInspectorGUI();
            if (_buttonMethods == null || _sliderMethods == null) return;
            var yellowLabelStyle = new GUIStyle(EditorStyles.label) {normal = {textColor = Color.yellow}};
            if (_buttonMethods.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Test Buttons", yellowLabelStyle);
                foreach (var method in _buttonMethods)
                {
                    var attrTestButton = method.GetCustomAttribute<TestButtonAttribute>();
                    var buttonTxt = attrTestButton.Text;
                    if(string.IsNullOrEmpty(buttonTxt))  buttonTxt = method.Name;
                    if (GUILayout.Button(buttonTxt))
                    {
                        method.Invoke(target,null);
                    }
                }
            }
            
            if (_sliderMethods.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Test Sliders", yellowLabelStyle);
                foreach (var method in _sliderMethods)
                {
                    var attrTestSlider = method.GetCustomAttribute<TestSliderAttribute>();
                    var slider = _sliderMethodsParams[method];
                    slider = EditorGUILayout.Slider(slider, attrTestSlider.Min, attrTestSlider.Max);
                    _sliderMethodsParams[method] = slider;
                    if (GUILayout.Button($"{method.Name} ( {slider} )"))
                    {
                        method.Invoke(target, new object[] {slider});
                    }
                    GUILayout.Space(10);
                }
            }
        }
    }
}
#endif
