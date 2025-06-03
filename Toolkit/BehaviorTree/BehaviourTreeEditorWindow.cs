#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio
{
    // BehaviourTreeEditorWindow.cs

    public class BehaviourTreeEditorWindow : EditorWindow
    {
        private BehaviourTree tree;
        private Vector2 scrollPosition;

        [MenuItem("Window/AI/Behaviour Tree Editor")]
        public static void ShowWindow()
        {
            GetWindow<BehaviourTreeEditorWindow>("Behaviour Tree Editor");
        }

        private void OnGUI()
        {
            if (tree == null)
            {
                tree = Selection.activeObject as BehaviourTree;
            }

            if (tree == null)
            {
                EditorGUILayout.HelpBox("Select a Behaviour Tree asset to edit", MessageType.Info);
                return;
            }

            // 绘制节点
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var node in tree.nodes)
            {
                DrawNode(node);
            }
            EditorGUILayout.EndScrollView();

            // 绘制连接线
            DrawConnections();
        }

        private void DrawNode(BehaviourTreeNode node)
        {
            GUILayout.BeginArea(new Rect(node.position, new Vector2(200, 100)), 
                new GUIStyle("flow node 0"));
            
            EditorGUILayout.LabelField(node.GetType().Name);
            node.description = EditorGUILayout.TextArea(node.description);
            
            GUILayout.EndArea();
        }

        private void DrawConnections()
        {
            foreach (var node in tree.nodes)
            {
                if (node is CompositeNode compositeNode)
                {
                    foreach (var child in compositeNode.children)
                    {
                        DrawNodeConnection(node.position, child.position);
                    }
                }
            }
        }

        private void DrawNodeConnection(Vector2 start, Vector2 end)
        {
            Handles.DrawBezier(
                start + new Vector2(100, 50),
                end + new Vector2(100, 50),
                start + new Vector2(100, 50) + Vector2.right * 50f,
                end + new Vector2(100, 50) + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );
        }
    }
    #endif
}