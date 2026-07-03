using System;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private static bool IsInteractiveType(Type type)
        {
            return type == typeof(Button)
                   || type == typeof(Toggle)
                   || type == typeof(Slider)
                   || type == typeof(InputField)
                   || IsListUpdaterType(type);
        }

        private static bool IsListUpdaterType(Type type)
        {
            return type != null && typeof(IListUpdater).IsAssignableFrom(type);
        }

        private static bool HasListItemComponent(UnityEngine.Transform transform)
        {
            return transform != null && transform.GetComponents<UnityEngine.Component>().Any(component => component is IListItem);
        }

        private static bool IsCloseButton(GeneratedFieldInfo field)
        {
            return field.interactionComponentType == typeof(Button)
                   && field.fieldName.Contains("close", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDefaultSelectedComponent(Type componentType, Type targetType)
        {
            if (componentType == null || targetType == null) return false;
            return componentType == targetType || targetType.IsAssignableFrom(componentType);
        }

        private static string GetPrefixByType(Type type, bool toLower)
        {
            if (ComponentPrefixes.TryGetValue(type.Name, out var prefix)) return toLower ? prefix.ToLower() : prefix;
            return string.Empty;
        }

        private static string GetTypeName(Type type)
        {
            if (type == typeof(IListUpdater)) return "IListUpdater";
            if (type == typeof(TMPro.TextMeshProUGUI)) return "TextMeshProUGUI";
            if (type.Namespace == "UnityEngine" || type.Namespace == "UnityEngine.UI" || type.Namespace == "TMPro" || type.Namespace == "PowerCellStudio") return type.Name;
            if (!string.IsNullOrEmpty(type.FullName)) return type.FullName.Replace('+', '.');
            return type.Name;
        }

        private static string MakeValidTypeName(string input)
        {
            var value = MakeValidVariableName(input);
            if (string.IsNullOrEmpty(value)) return "GeneratedUIWindow";
            if (char.IsDigit(value[0])) value = $"UI{value}";
            return value;
        }

        private static string MakeValidVariableName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            foreach (var prefix in NamePrefixes)
            {
                if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(prefix.Length);
                    break;
                }
            }

            var sb = new StringBuilder();
            var capitalizeNext = true;
            foreach (var c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }
            return sb.ToString();
        }
    }
}