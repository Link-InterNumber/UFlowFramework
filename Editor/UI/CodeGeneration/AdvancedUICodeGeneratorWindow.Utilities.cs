using System;
using System.Text;

namespace PowerCellStudio
{
    public partial class AdvancedUICodeGeneratorWindow
    {
        private static string GetPrefixByType(Type type, bool toLower)
        {
            if (ComponentPrefixes.TryGetValue(type.Name, out var prefix)) return toLower ? prefix.ToLower() : prefix;
            return string.Empty;
        }

        private static string GetTypeName(Type type)
        {
            if (type == typeof(IListUpdater)) return "IListUpdater";
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