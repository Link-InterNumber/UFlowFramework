using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    [CustomEditor(typeof(ObjectInteractor), true)]

    public class ObjectInteractorEditor: Editor
    {
        private static string _helpContent =
            "未检测到碰撞体。该对象可能无法参与物交互。\nNo collider detected. This object may not be able to participate in physical interactions.";
        
        public override void OnInspectorGUI()
        {
            var interactor = (ObjectInteractor)target;
            var hasCollider =  interactor.gameObject.GetComponent<Collider>() != null 
                               || interactor.gameObject.GetComponent<Collider2D>() != null;
            if (!hasCollider)
            {
                // 绘制提醒窗口
                EditorGUILayout.HelpBox(_helpContent, MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add BoxCollider"))
                    {
                        Undo.AddComponent<BoxCollider>(interactor.gameObject);
                        EditorUtility.SetDirty(interactor.gameObject);
                    }

                    if (GUILayout.Button("Add BoxCollider2D"))
                    {
                        Undo.AddComponent<BoxCollider2D>(interactor.gameObject);
                        EditorUtility.SetDirty(interactor.gameObject);
                    }
                }
            }
            base.OnInspectorGUI();
        }
    }
}