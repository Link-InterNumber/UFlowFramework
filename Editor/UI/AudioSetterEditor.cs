#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    [CustomEditor(typeof(AudioSetter), true)]
    [CanEditMultipleObjects]
    public class AudioSetterEditor: Editor
    {
        private SerializedProperty m_clipRef;
        private SerializedProperty m_audioType;
        private SerializedProperty m_musicGroup;
        private SerializedProperty m_playOnEnable;
        private SerializedProperty m_playOnUp;
        private SerializedProperty m_playOnDown;
        private SerializedProperty m_attachToGameObject;
        private SerializedProperty m_fadeoutTime;
        private SerializedProperty m_intervalTime;
        private SerializedProperty m_fadeinTime;
        private AudioClip m_obj = null;

        AudioSetter m_target => target as AudioSetter;
        private bool _hasTrigger;

        private void OnEnable()
        {
            m_clipRef = serializedObject.FindProperty("clipRef");
            m_audioType = serializedObject.FindProperty("audioType");
            m_musicGroup = serializedObject.FindProperty("musicGroup");
            m_playOnEnable = serializedObject.FindProperty("playOnEnable");
            m_playOnUp = serializedObject.FindProperty("playOnUp");
            m_playOnDown = serializedObject.FindProperty("playOnDown");
            m_attachToGameObject = serializedObject.FindProperty("attachToGameObject");
            m_fadeoutTime = serializedObject.FindProperty("fadeoutTime");
            m_intervalTime = serializedObject.FindProperty("intervalTime");
            m_fadeinTime = serializedObject.FindProperty("fadeinTime");
            _hasTrigger = m_target.GetComponent<AudioTrigger>();
        }

        private void OnDisable()
        {
            m_obj = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (!string.IsNullOrEmpty(m_clipRef.stringValue) && m_obj == null)
            {
                m_obj = AssetDatabase.LoadAssetAtPath<AudioClip>(m_clipRef.stringValue);
            }
            m_obj = (AudioClip)EditorGUILayout.ObjectField("AudioClip", m_obj, typeof(AudioClip), false);
            if (m_obj)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_obj, out string guid, out long _))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    m_clipRef.stringValue = path;                    
                }
            }
            else
            {
                m_clipRef.stringValue = string.Empty;
            }
            EditorGUILayout.PropertyField(m_clipRef);
            EditorGUILayout.PropertyField(m_audioType);
            var audioType = (AudioSourceType) m_audioType.enumValueIndex;
            // var musicGroup = (MusicGroup) m_musicGroup.enumValueIndex;
            if (audioType == AudioSourceType.Music || audioType == AudioSourceType.Ambience)
            {
                EditorGUILayout.PropertyField(m_musicGroup);
                EditorGUILayout.PropertyField(m_fadeoutTime);
                EditorGUILayout.PropertyField(m_intervalTime);
                EditorGUILayout.PropertyField(m_fadeinTime);
            }
            if (m_target.GetComponent<UIBehaviour>())
            {
                EditorGUILayout.PropertyField(m_playOnUp);
                EditorGUILayout.PropertyField(m_playOnDown);
            }
            else if (!_hasTrigger && GUILayout.Button("Add AudioTrigger"))
            {
                var trigger = m_target.gameObject.GetComponent<AudioTrigger>();
                if(!trigger) trigger = m_target.gameObject.AddComponent<AudioTrigger>();
                trigger.audioSetter = m_target;
                m_playOnEnable.boolValue = false;
                m_playOnUp.boolValue = false;
                m_playOnDown.boolValue = false;
                _hasTrigger = true;
            }
            EditorGUILayout.PropertyField(m_attachToGameObject);
            EditorGUILayout.PropertyField(m_playOnEnable);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif