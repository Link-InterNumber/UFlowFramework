#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class ButtonExMenu
    {
        [MenuItem("GameObject/UI/ButtonEx", false, 2000)]
        public static void AddButton(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("ButtonEx");
            var button = CreateButton(go);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            GameObject parent = menuCommand.context as GameObject;
            if (parent != null)
            {
                string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent.transform, go.name);
                go.name = uniqueName;
                Undo.SetTransformParent(go.transform, parent.transform, "Parent " + go.name);
                GameObjectUtility.SetParentAndAlign(go, parent);
            }
            Selection.activeGameObject = go;
        }

        static ButtonEx CreateButton(GameObject gameObject)
        {
            var button = gameObject.AddComponent<ButtonEx>();
            button.transition = Selectable.Transition.None;
            gameObject.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            var image = gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var textgo = new GameObject("Text");
            textgo.transform.SetParent(gameObject.transform);
            textgo.transform.localPosition = Vector3.zero;
            textgo.AddComponent<RectTransform>();
            var textRect = textgo.transform as RectTransform;
            textRect.Adapt2Parent();
            var text = textgo.AddComponent<TextEx>();
            text.fontSize = 20;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "ButtonEx";
            text.raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/FrameWork/Fonts/字魂扁桃体.ttf");
            if (font) text.font = font;
            return button;
        }
    }

    [CustomEditor(typeof(ButtonEx), true)]
    [CanEditMultipleObjects]
    public class ButtonExEditor : ButtonEditor
    {
        private SerializedProperty m_enableLongPress;
        private SerializedProperty m_longPressRepeat;
        private SerializedProperty m_longPressStartTime;
        private SerializedProperty m_longPressTriggerTime;
        private SerializedProperty m_longPressIntervalTime;
        private SerializedProperty m_onLongPress;
        private SerializedProperty m_onLongPressUp;
        
        ButtonEx m_target => target as ButtonEx;

        private bool _hasUpTrigger;
        private bool _hasDownTrigger;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_enableLongPress = serializedObject.FindProperty("enableLongPress");
            m_longPressRepeat = serializedObject.FindProperty("longPressRepeat");
            m_longPressStartTime = serializedObject.FindProperty("longPressStartTime");
            m_longPressTriggerTime = serializedObject.FindProperty("longPressTriggerTime");
            m_longPressIntervalTime = serializedObject.FindProperty("longPressIntervalTime");
            m_onLongPress = serializedObject.FindProperty("onLongPress");
            m_onLongPressUp = serializedObject.FindProperty("onLongPressUp");
            CheckTrigger();
        }

        private void CheckTrigger()
        {
            var triggers = m_target.GetComponents<AudioSetter>();
            foreach (var audioSetter in triggers)
            {
                if(audioSetter.playOnUp) _hasUpTrigger = true;
                if(audioSetter.playOnDown) _hasDownTrigger = true;
            }
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_enableLongPress);
            if (m_enableLongPress.boolValue)
            {
                EditorGUILayout.PropertyField(m_longPressRepeat);
                EditorGUILayout.PropertyField(m_longPressStartTime);
                EditorGUILayout.PropertyField(m_longPressTriggerTime);
                if (m_longPressRepeat.boolValue)
                {
                    EditorGUILayout.PropertyField(m_longPressIntervalTime);
                }
                EditorGUILayout.PropertyField(m_onLongPress);
                EditorGUILayout.PropertyField(m_onLongPressUp);
            }
            if (!_hasUpTrigger && GUILayout.Button("Add Up AudioTrigger"))
            {
                CheckTrigger();
                if (!_hasUpTrigger)
                {
                    var trigger = m_target.gameObject.AddComponent<AudioSetter>();
                    trigger.playOnUp = true;
                    trigger.playOnDown = false;
                    trigger.musicGroup = MusicGroup.UI;
                    trigger.audioType = AudioSourceType.UIEffect;
                    trigger.playOnEnable = false;
                }
                _hasUpTrigger = true;
            }
            if (!_hasDownTrigger && GUILayout.Button("Add Down AudioTrigger"))
            {
                CheckTrigger();
                if (!_hasDownTrigger)
                {
                    var trigger = m_target.gameObject.AddComponent<AudioSetter>();
                    trigger.playOnUp = false;
                    trigger.playOnDown = true;
                    trigger.musicGroup = MusicGroup.UI;
                    trigger.audioType = AudioSourceType.UIEffect;
                    trigger.playOnEnable = false;
                }
                _hasDownTrigger = true;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif