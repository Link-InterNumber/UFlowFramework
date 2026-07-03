using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryBindPendingPrefabComponent;
        }

        private static void TryBindPendingPrefabComponent()
        {
            var pendingBind = LoadPendingBindInfo();
            if (pendingBind == null) return;

            if (TryBindPrefabComponent(pendingBind, false))
            {
                ClearPendingBindInfo();
            }
        }

        private static void SavePendingBindInfo(PendingBindInfo pendingBind)
        {
            if (pendingBind == null)
            {
                ClearPendingBindInfo();
                return;
            }

            SessionState.SetString(PendingBindInfoSessionStateKey, EditorJsonUtility.ToJson(pendingBind));
        }

        private static PendingBindInfo LoadPendingBindInfo()
        {
            var json = SessionState.GetString(PendingBindInfoSessionStateKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonUtility.FromJson<PendingBindInfo>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load pending UI prefab bind info: {exception.Message}");
                ClearPendingBindInfo();
                return null;
            }
        }

        private static void ClearPendingBindInfo()
        {
            SessionState.EraseString(PendingBindInfoSessionStateKey);
        }

        private PendingBindInfo CreateBindInfo(string className, List<GeneratedFieldInfo> selectedFields)
        {
            return new PendingBindInfo
            {
                prefabPath = _prefabPath,
                namespaceName = _namespaceName,
                className = className,
                nodes = selectedFields.Select(node => new PendingBindNodeInfo
                {
                    relativePath = node.relativePath,
                    fieldName = node.fieldName,
                    fieldTypeName = GetTypeName(node.fieldType),
                    fieldTypeAssemblyQualifiedName = node.fieldType.AssemblyQualifiedName,
                    isCloseButton = IsCloseButton(node)
                }).ToList()
            };
        }

        private void TryBindCurrentPrefabComponent()
        {
            var selectedFields = GetSelectedFields();
            var bindInfo = CreateBindInfo(MakeValidTypeName(_className), selectedFields);
            TryBindPrefabComponent(bindInfo, true);
        }

        private static bool TryBindPrefabComponent(PendingBindInfo pendingBind, bool showDialog)
        {
            if (pendingBind == null || string.IsNullOrEmpty(pendingBind.prefabPath) || string.IsNullOrEmpty(pendingBind.className))
            {
                if (showDialog) EditorUtility.DisplayDialog("Bind Failed", "Prefab path or class name is empty.", "OK");
                return false;
            }

            var windowType = FindWindowType(pendingBind.FullClassName, pendingBind.className);
            if (windowType == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Bind Failed", $"Cannot find generated UIWindow type: {pendingBind.FullClassName}\nPlease wait for scripts to compile, then click the bind button again.", "OK");
                }
                else
                {
                    Debug.LogWarning($"Skip prefab binding because generated type is not available yet: {pendingBind.FullClassName}");
                }
                return false;
            }
            if (!typeof(UIWindow).IsAssignableFrom(windowType))
            {
                if (showDialog) EditorUtility.DisplayDialog("Bind Failed", $"Type {pendingBind.FullClassName} does not inherit UIWindow.", "OK");
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pendingBind.prefabPath);
            if (prefab == null)
            {
                if (showDialog) EditorUtility.DisplayDialog("Bind Failed", $"Cannot load prefab: {pendingBind.prefabPath}", "OK");
                return false;
            }

            var uiComp = prefab.GetComponent(windowType) ?? prefab.AddComponent(windowType);
            var serializedObject = new SerializedObject(uiComp);
            var closeButtons = new List<Button>();
            var boundCount = 0;
            foreach (var node in pendingBind.nodes)
            {
                var nodeTransform = prefab.transform.Find(node.relativePath);
                if (nodeTransform == null) continue;

                var component = GetBindComponent(nodeTransform, node.fieldTypeName, node.fieldTypeAssemblyQualifiedName);
                if (component == null) continue;

                var field = windowType.GetField(node.fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType.IsAssignableFrom(component.GetType()))
                {
                    var property = serializedObject.FindProperty(node.fieldName);
                    if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        property.objectReferenceValue = component;
                        boundCount++;
                    }
                    else
                    {
                        field.SetValue(uiComp, component);
                        boundCount++;
                    }
                }

                if (node.isCloseButton && component is Button closeButton)
                {
                    closeButtons.Add(closeButton);
                }
            }

            if (closeButtons.Count > 0)
            {
                var closeBtnProperty = serializedObject.FindProperty("closeBtn");
                if (closeBtnProperty != null && closeBtnProperty.isArray)
                {
                    closeBtnProperty.arraySize = closeButtons.Count;
                    for (var i = 0; i < closeButtons.Count; i++)
                    {
                        closeBtnProperty.GetArrayElementAtIndex(i).objectReferenceValue = closeButtons[i];
                    }
                }
                else
                {
                    var closeField = typeof(UIWindow).GetField("closeBtn", BindingFlags.Public | BindingFlags.Instance);
                    closeField?.SetValue(uiComp, closeButtons.ToArray());
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
            EditorUtility.SetDirty(uiComp);
            PrefabUtility.SavePrefabAsset(prefab);
            AssetDatabase.Refresh();
            Debug.Log($"UI component [{pendingBind.FullClassName}] added and bound to prefab: {pendingBind.prefabPath}, bound fields: {boundCount}, close buttons: {closeButtons.Count}");
            if (showDialog) EditorUtility.DisplayDialog("Bind Success", $"UI component [{pendingBind.FullClassName}] has been added and bound to prefab.\nBound fields: {boundCount}\nClose buttons: {closeButtons.Count}", "OK");
            return true;
        }

        private static Component GetBindComponent(Transform transform, string fieldTypeName, string fieldTypeAssemblyQualifiedName)
        {
            if (fieldTypeName == nameof(RectTransform)) return transform as RectTransform;
            if (fieldTypeName == nameof(Button)) return transform.GetComponent<Button>();
            if (fieldTypeName == nameof(Toggle)) return transform.GetComponent<Toggle>();
            if (fieldTypeName == nameof(Slider)) return transform.GetComponent<Slider>();
            if (fieldTypeName == nameof(InputField)) return transform.GetComponent<InputField>();
            if (fieldTypeName == nameof(IListUpdater)) return transform.GetComponents<Component>().FirstOrDefault(component => component is IListUpdater);
            if (fieldTypeName == nameof(Image)) return transform.GetComponent<Image>();
            if (fieldTypeName == nameof(Text)) return transform.GetComponent<Text>();
            if (fieldTypeName == nameof(TMPro.TextMeshProUGUI)) return transform.GetComponent<TMPro.TextMeshProUGUI>();
            var fieldType = Type.GetType(fieldTypeAssemblyQualifiedName);
            return fieldType == null ? null : transform.GetComponent(fieldType);
        }

        private static Type FindWindowType(string fullClassName, string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullClassName);
                if (type != null) return type;
            }

            return TypeCache.GetTypesDerivedFrom<UIWindow>().FirstOrDefault(type => type.Name == className);
        }
    }
}