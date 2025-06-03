#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    public class ACTEditorWindow : EditorWindow
    {
        private ACTConfig config;
        private Vector2 scrollPos;
        private ACTAction selectedAction;

        [MenuItem("Window/ACT Editor")]
        public static void ShowWindow() => GetWindow<ACTEditorWindow>("ACT Editor");

        private void OnGUI()
        {
            config = (ACTConfig)EditorGUILayout.ObjectField("ACT Config", config, typeof(ACTConfig), false);

            if (config == null)
            {
                EditorGUILayout.HelpBox("Create or select an ACT Config asset", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            
            // 左侧动作列表
            DrawActionList();
            
            // 右侧编辑区域
            DrawEditArea();
            
            EditorGUILayout.EndHorizontal();
        }

        void DrawActionList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var action in config.actions)
            {
                if (GUILayout.Button(action.actionName))
                {
                    selectedAction = action;
                }
            }
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("+ New Action"))
            {
                var newAction = new ACTAction();
                newAction.actionName = "New Action";
                config.actions.Add(newAction);
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawEditArea()
        {
            EditorGUILayout.BeginVertical();
            
            if (selectedAction != null)
            {
                selectedAction.actionName = EditorGUILayout.TextField("Action Name", selectedAction.actionName);
                selectedAction.animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation", 
                    selectedAction.animationClip, typeof(AnimationClip), false);
                
                // 时间轴编辑
                EditorGUILayout.LabelField("Hit Frame Timeline");
                DrawTimeline(selectedAction);
                
                // 过渡条件
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Transitions");
                DrawTransitions(selectedAction);
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawTimeline(ACTAction action)
        {
            // 简化的时间轴绘制
            Rect timelineRect = GUILayoutUtility.GetRect(600, 100);
            GUI.Box(timelineRect, GUIContent.none);
            
            if (action.animationClip != null)
            {
                float frameWidth = timelineRect.width / action.animationClip.length / 60f;
                
                // 绘制帧标记
                for (int i = 0; i < action.animationClip.length * 60; i++)
                {
                    float xPos = i * frameWidth;
                    GUI.Label(new Rect(timelineRect.x + xPos, timelineRect.y, 20, 20), i.ToString());
                }
                
                // 绘制命中框标记
                foreach (var hitFrame in action.hitFrames)
                {
                    float xPos = hitFrame.frame * frameWidth;
                    EditorGUI.DrawRect(new Rect(timelineRect.x + xPos, timelineRect.y + 30, 5, 20), Color.red);
                }
            }
        }

        void DrawTransitions(ACTAction action)
        {
            foreach (var transition in action.transitions)
            {
                EditorGUILayout.BeginHorizontal();
                transition.inputType = (InputType)EditorGUILayout.EnumPopup(transition.inputType);
                transition.targetAction = EditorGUILayout.TextField(transition.targetAction);
                transition.priority = EditorGUILayout.IntField("Priority", transition.priority);
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("Add Transition"))
            {
                action.transitions.Add(new TransitionCondition());
            }
        }
    }
}

#endif