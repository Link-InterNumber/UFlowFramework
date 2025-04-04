#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    [CustomEditor(typeof(PIDCurveDisplayer))]
    public class PIDCurveInspector: Editor
    {
        private PIDCurveDisplayer _curve;
        private SerializedProperty _curveDataProp;

        private void OnEnable()
        {
            _curve = target as PIDCurveDisplayer;
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
                var v = -1f;
                for (int i = 0; i < 300; i++)
                {
                    v = v + _curve.OnGUIUpdateValue(dt, v);
                    curve.AddKey(new Keyframe((i+1) * dt, v, 0f,0f));
                }
                EditorGUILayout.Space(220);
                EditorGUI.CurveField(new Rect(3,125,500,200), curve);
            }
        }
    }
}
#endif
