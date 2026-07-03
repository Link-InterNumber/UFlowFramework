using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Interactive"))
            {
                foreach (var node in _nodes)
                {
                    node.selected = node.interactionComponentType != null;
                }
            }
            if (GUILayout.Button("Select All"))
            {
                foreach (var node in _nodes)
                {
                    node.selected = true;
                }
            }
            if (GUILayout.Button("Clear"))
            {
                foreach (var node in _nodes)
                {
                    node.selected = false;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNodeTree()
        {
            EditorGUILayout.LabelField("Prefab Nodes", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");
            foreach (var node in _nodes)
            {
                DrawNode(node);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(NodeInfo node)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(node.depth * 18f);
            node.selected = EditorGUILayout.Toggle(node.selected, GUILayout.Width(18));
            EditorGUILayout.LabelField(node.transform.name, GUILayout.MinWidth(120));

            var components = node.GetComponentDisplay();
            if (!string.IsNullOrEmpty(components))
            {
                var color = GUI.contentColor;
                GUI.contentColor = node.interactionComponentType != null ? Color.cyan : Color.gray;
                EditorGUILayout.LabelField(components, GUILayout.MinWidth(160));
                GUI.contentColor = color;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void BuildNodeInfos()
        {
            _nodes.Clear();
            foreach (Transform child in _prefab.transform)
            {
                CollectNode(child, string.Empty, 0);
            }
            ResolveDuplicateFieldNames();
        }

        private void CollectNode(Transform transform, string parentPath, int depth)
        {
            var relativePath = string.IsNullOrEmpty(parentPath) ? transform.name : $"{parentPath}/{transform.name}";
            var interactionType = FindTargetComponentType(transform);
            var info = new NodeInfo
            {
                transform = transform,
                relativePath = relativePath,
                depth = depth,
                interactionComponentType = interactionType,
                selected = interactionType != null
            };
            info.fieldType = info.interactionComponentType ?? typeof(RectTransform);
            info.fieldName = GetPrefixByType(info.fieldType, true) + MakeValidVariableName(transform.name);
            info.methodName = GetPrefixByType(info.fieldType, false) + MakeValidVariableName(transform.name);
            _nodes.Add(info);

            foreach (Transform child in transform)
            {
                CollectNode(child, relativePath, depth + 1);
            }
        }

        private Type FindTargetComponentType(Transform transform)
        {
            foreach (var type in TargetComponentTypes)
            {
                if (transform.GetComponent(type) != null) return type;
            }
            return null;
        }

        private void ResolveDuplicateFieldNames()
        {
            var groups = _nodes.GroupBy(node => node.fieldName).Where(group => group.Count() > 1);
            foreach (var group in groups)
            {
                var index = 1;
                foreach (var node in group)
                {
                    node.fieldName = $"{node.fieldName}{index}";
                    node.methodName = $"{node.methodName}{index}";
                    index++;
                }
            }
        }
    }
}