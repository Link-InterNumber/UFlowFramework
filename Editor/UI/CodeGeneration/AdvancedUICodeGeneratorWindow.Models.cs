using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private class NodeInfo
        {
            public Transform transform;
            public string relativePath;
            public int depth;
            public Type interactionComponentType;
            public List<ComponentFieldInfo> fields = new List<ComponentFieldInfo>();

            public bool HasSelectedField => fields.Any(field => field.selected);
        }

        private class ComponentFieldInfo
        {
            public Type fieldType;
            public Component component;
            public bool selected;
            public string baseFieldName;
            public string baseMethodName;
            public string fieldName;
            public string methodName;
        }

        private class GeneratedFieldInfo
        {
            public NodeInfo node;
            public ComponentFieldInfo field;

            public string relativePath => node.relativePath;
            public Type interactionComponentType => IsInteractiveType(field.fieldType) ? field.fieldType : null;
            public Type fieldType => field.fieldType;
            public string fieldName => field.fieldName;
            public string methodName => field.methodName;
        }

        private class ScriptFileInfo
        {
            public readonly string assetPath;
            public readonly string content;
            public readonly bool openAfterGenerate;

            public string AssetPath => assetPath;

            public ScriptFileInfo(string assetPath, string content, bool openAfterGenerate)
            {
                this.assetPath = assetPath;
                this.content = content;
                this.openAfterGenerate = openAfterGenerate;
            }
        }

        [Serializable]
        private class PendingBindInfo
        {
            public string prefabPath;
            public string namespaceName;
            public string className;
            public List<PendingBindNodeInfo> nodes = new List<PendingBindNodeInfo>();

            public string FullClassName => string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";
        }

        [Serializable]
        private class PendingBindNodeInfo
        {
            public string relativePath;
            public string fieldName;
            public string fieldTypeName;
            public string fieldTypeAssemblyQualifiedName;
            public bool isCloseButton;
        }
    }
}