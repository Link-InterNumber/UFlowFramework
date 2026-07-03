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
            public bool selected;
            public Type fieldType;
            public Type interactionComponentType;
            public string fieldName;
            public string methodName;

            public string GetComponentDisplay()
            {
                var components = transform.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().Name)
                    .ToArray();
                return components.Length == 0 ? string.Empty : string.Join(", ", components);
            }
        }

        private class ScriptFileInfo
        {
            public readonly string className;
            public readonly string assetPath;
            public readonly string content;
            public readonly bool openAfterGenerate;

            public string AssetPath => assetPath;

            public ScriptFileInfo(string className, string assetPath, string content, bool openAfterGenerate)
            {
                this.className = className;
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
            public bool isCloseButton;
        }
    }
}