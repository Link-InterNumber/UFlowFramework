#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    [CustomEditor(typeof(ProceduralCurveDisplayer))]
    public class ProceduralCurveInspector: Editor
    {
        private ProceduralCurveDisplayer _curve;
        private SerializedProperty _curveDataProp;

        private void OnEnable()
        {
            _curve = target as ProceduralCurveDisplayer;
            _curveDataProp = serializedObject.FindProperty("curveData");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if(_curve.curveData == null) return;
            if (_curveDataProp.isExpanded)
            {
                var curve = new AnimationCurve();
                _curve.OnGUIInit();
                curve.AddKey(new Keyframe(0f, -1f, 0f,0f));
                var dt = 1f / 60f;
                for (int i = 0; i < 300; i++)
                {
                    var newValue = _curve.OnGUIUpdateValue(dt, 0f);
                    curve.AddKey(new Keyframe((i+1) * dt, newValue, 0f,0f));
                }
                EditorGUILayout.Space(220);
                EditorGUI.CurveField(new Rect(3,125,500,200), curve);
            }
        }
    }
}
#endif
