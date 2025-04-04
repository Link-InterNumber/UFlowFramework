#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    [CustomEditor(typeof(EaseCurveDisplayer))]
    public class EaseCurveInspector: Editor
    {
        private EaseCurveDisplayer _curve;
        private SerializedProperty _curveDataProp;

        private void OnEnable()
        {
            _curve = target as EaseCurveDisplayer;
            _curveDataProp = serializedObject.FindProperty("easeType");
        }

        private void OnDisable()
        {
            _curve = null;
            _curveDataProp = null;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 0f, 0f,0f));
            var dt = 1f / 60f;
            for (int i = 0; i < 300; i++)
            {
                var v = _curve.OnGUIUpdateValue(i / 300f);
                curve.AddKey(new Keyframe((i+1) * dt, v, 0f,0f));
            }

            EditorGUILayout.Space(200);
            EditorGUI.CurveField(new Rect(3,50,500,200), curve);
        }
    }
}
#endif
