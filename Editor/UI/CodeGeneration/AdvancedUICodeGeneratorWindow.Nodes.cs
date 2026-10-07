using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private static readonly Color SelectedNodeColor = new Color(0.42f, 0.78f, 1f, 1f);

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Component Selection", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Interactive", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                {
                    SetAllFieldSelection(field => IsInteractiveType(field.fieldType));
                }
                if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                {
                    SetAllFieldSelection(_ => true);
                }
                if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                {
                    SetAllFieldSelection(_ => false);
                }
            }
        }

        private void SetAllFieldSelection(Func<ComponentFieldInfo, bool> selector)
        {
            foreach (var field in _nodes.SelectMany(node => node.fields))
            {
                field.selected = selector(field);
            }

            ResolveDuplicateFieldNames();
        }

        private void DrawNodeTree()
        {
            using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox, GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Prefab Nodes", EditorUIStyle.SectionTitle);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"{_nodes.Count} nodes", EditorUIStyle.MutedLabel, GUILayout.Width(70f));
                }

                EditorUIStyle.DrawSeparator();
                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition, EditorUIStyle.SectionBox, GUILayout.ExpandHeight(true)))
                {
                    _scrollPosition = scroll.scrollPosition;
                    if (_nodes.Count == 0)
                    {
                        EditorGUILayout.LabelField("No child nodes were found in this prefab.", EditorUIStyle.MutedLabel);
                    }
                    else
                    {
                        foreach (var node in _nodes)
                        {
                            DrawNode(node);
                        }
                    }
                }
            }
        }

        private void DrawNode(NodeInfo node)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var indentWidth = node.depth * EditorUIStyle.NodeIndentWidth;
                GUILayout.Space(indentWidth);

                var icon = EditorGUIUtility.IconContent(node.depth == 0 ? "Prefab Icon" : "GameObject Icon");
                GUILayout.Label(icon, GUILayout.Width(18f), GUILayout.Height(EditorGUIUtility.singleLineHeight));

                var previousColor = GUI.contentColor;
                GUI.contentColor = node.HasSelectedField ? SelectedNodeColor : previousColor;
                EditorGUILayout.LabelField(node.transform.name,
                    GUILayout.Width(GetNodeNameWidth(indentWidth, node.fields.Count)));
                GUI.contentColor = previousColor;

                if (node.fields.Count > 0)
                {
                    var componentToggleWidth = GetComponentToggleWidth(indentWidth, node.fields.Count);
                    GUILayout.FlexibleSpace();
                    foreach (var field in node.fields)
                    {
                        EditorGUI.BeginChangeCheck();
                        field.selected = DrawComponentToggle(field, componentToggleWidth);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ResolveDuplicateFieldNames();
                        }
                    }
                }
            }
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
            var isListItemNode = HasListItemComponent(transform);
            var interactionType = FindTargetComponentType(transform);
            var info = new NodeInfo
            {
                transform = transform,
                relativePath = relativePath,
                depth = depth,
                interactionComponentType = interactionType
            };
            if (!isListItemNode)
            {
                BuildComponentFields(info);
            }
            _nodes.Add(info);

            if (isListItemNode) return;

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

        private void BuildComponentFields(NodeInfo node)
        {
            var components = node.transform.GetComponents<Component>()
                .Where(component => component != null && !IgnoredComponentTypes.Contains(component.GetType()))
                .OrderBy(component => component is RectTransform ? 1 : 0)
                .ThenBy(component => component.GetType().Name)
                .ToArray();

            foreach (var component in components)
            {
                node.fields.Add(CreateFieldInfo(node, component, IsDefaultSelectedComponent(component.GetType(), node.interactionComponentType)));
            }
        }

        private ComponentFieldInfo CreateFieldInfo(NodeInfo node, Component component, bool selected)
        {
            var fieldType = component.GetType();
            var field = new ComponentFieldInfo
            {
                fieldType = fieldType,
                component = component,
                selected = selected,
                baseFieldName = GetPrefixByType(fieldType, true) + MakeValidVariableName(node.transform.name),
                baseMethodName = GetPrefixByType(fieldType, false) + MakeValidVariableName(node.transform.name)
            };
            field.fieldName = field.baseFieldName;
            field.methodName = field.baseMethodName;
            return field;
        }

        private float GetNodeNameWidth(float indentWidth, int componentCount)
        {
            var windowWidth = Mathf.Max(position.width, 360f);
            var preferredWidth = componentCount > 0 ? windowWidth * 0.28f : windowWidth - indentWidth - 32f;
            return Mathf.Clamp(preferredWidth, EditorUIStyle.MinNodeNameWidth, EditorUIStyle.MaxNodeNameWidth);
        }

        private float GetComponentToggleWidth(float indentWidth, int componentCount)
        {
            if (componentCount <= 0) return EditorUIStyle.MaxComponentToggleWidth;

            var nodeNameWidth = GetNodeNameWidth(indentWidth, componentCount);
            var availableWidth = position.width - indentWidth - nodeNameWidth - 42f;
            var width = availableWidth / componentCount;
            return Mathf.Clamp(width, EditorUIStyle.MinComponentToggleWidth, EditorUIStyle.MaxComponentToggleWidth);
        }

        private static bool DrawComponentToggle(ComponentFieldInfo field, float width)
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(width));
            if (field.selected)
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 2f),
                    new Color(EditorUIStyle.AccentColor.r, EditorUIStyle.AccentColor.g, EditorUIStyle.AccentColor.b, 0.18f));
            }

            var toggleRect = new Rect(rect.x + 2f, rect.y, 16f, rect.height);
            var value = EditorGUI.Toggle(toggleRect, field.selected);
            var icon = EditorGUIUtility.ObjectContent(field.component, field.fieldType).image;
            var iconRect = new Rect(rect.x + 18f, rect.y + 1f, 16f, 16f);
            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            var labelRect = new Rect(rect.x + 38f, rect.y, rect.width - 38f, rect.height);
            EditorGUI.LabelField(labelRect, GetTypeName(field.fieldType));
            return value;
        }

        private List<GeneratedFieldInfo> GetSelectedFields()
        {
            return _nodes
                .SelectMany(node => node.fields
                    .Where(field => field.selected)
                    .Select(field => new GeneratedFieldInfo { node = node, field = field }))
                .ToList();
        }

        private void ResolveDuplicateFieldNames()
        {
            foreach (var field in _nodes.SelectMany(node => node.fields))
            {
                field.fieldName = field.baseFieldName;
                field.methodName = field.baseMethodName;
            }

            var selectedFields = _nodes
                .SelectMany(node => node.fields.Where(field => field.selected))
                .ToList();
            var fieldNameCounts = new Dictionary<string, int>();
            var methodNameCounts = new Dictionary<string, int>();
            foreach (var field in selectedFields)
            {
                field.fieldName = GetUniqueName(field.baseFieldName, fieldNameCounts);
                field.methodName = GetUniqueName(field.baseMethodName, methodNameCounts);
            }
        }

        private static string GetUniqueName(string baseName, Dictionary<string, int> usedNames)
        {
            if (!usedNames.ContainsKey(baseName))
            {
                usedNames.Add(baseName, 1);
                return baseName;
            }

            var index = usedNames[baseName] + 1;
            string uniqueName;
            do
            {
                uniqueName = $"{baseName}{index}";
                index++;
            } while (usedNames.ContainsKey(uniqueName));

            usedNames[baseName] = index - 1;
            usedNames.Add(uniqueName, 1);
            return uniqueName;
        }
    }
}