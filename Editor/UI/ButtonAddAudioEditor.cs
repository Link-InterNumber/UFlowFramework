#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [CustomEditor(typeof(Button), true)]
    [CanEditMultipleObjects]
    public class ButtonAddAudioEditor : ButtonEditor
    {
        private bool _hasUpTrigger;
        private bool _hasDownTrigger;
        Button m_target => target as Button;
        protected override void OnEnable()
        {
            base.OnEnable();
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