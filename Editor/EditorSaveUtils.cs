using UnityEditor;

namespace PowerCellStudio
{
    public static class EditorSaveUtils
    {
        public static void SetEditorPref(string key, string value)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            EditorPrefs.SetString(saveKey, value);
        }

        public static string GetEditorPref(string key, string defaultValue)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            return EditorPrefs.GetString(saveKey, defaultValue);
        }
        
        public static void SetEditorPref(string key, int value)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            EditorPrefs.SetInt(saveKey, value);
        }

        public static int GetEditorPref(string key, int defaultValue)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            return EditorPrefs.GetInt(saveKey, defaultValue);
        }

        public static void SetEditorPref(string key, float value)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            EditorPrefs.SetFloat(saveKey, value);
        }

        public static float GetEditorPref(string key, float defaultValue)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            return EditorPrefs.GetFloat(saveKey, defaultValue);
        }

        public static void SetEditorPref(string key, bool value)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            EditorPrefs.SetBool(saveKey, value);
        }

        public static bool GetEditorPref(string key, bool defaultValue)
        {
            var saveKey = $"{PlayerSettings.productName}_{key}";
            return EditorPrefs.GetBool(saveKey, defaultValue);
        }
    } 
}