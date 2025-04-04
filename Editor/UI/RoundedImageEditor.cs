#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

namespace PowerCellStudio
{
    [CustomEditor(typeof(RoundedImage), true)]
    public class RoundedImageEditor : ImageEditor
    {
        private SerializedProperty _sprite;
        private SerializedProperty _cornerRadius;
        private SerializedProperty _cornerSegments;

        protected override void OnEnable()
        {
            base.OnEnable();

            _sprite = serializedObject.FindProperty("m_Sprite");
            _cornerRadius = serializedObject.FindProperty("cornerRadius");
            _cornerSegments = serializedObject.FindProperty("cornerSegments");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();
            bool showNativeSize = _sprite.objectReferenceValue != null;
            m_ShowNativeSize.target = showNativeSize;
            MaskableControlsGUI();
            NativeSizeButtonGUI();
            EditorGUILayout.PropertyField(_cornerRadius);
            EditorGUILayout.PropertyField(_cornerSegments);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("leftTop"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rightTop"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("leftBottom"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rightBottom"));
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}