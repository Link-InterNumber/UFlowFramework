using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    public class ConstSettingItem : IEditorSettingWindowItem
    {
        public string itemName => "Const Setting";

        // 可编辑字段
        private List<int> resolution = new List<int>();
        private Vector2Int defaultResolution;
        private Vector2Int defaultUISize;
        private string fileEncryptionKey;
        private Language defaultLanguage;
        private string localizationStringTable;
        private string localizationAssetTable;
        private Dictionary<Language, string> languageFont = new Dictionary<Language, string>();
        private int millionInt;
        private long millionLong;
        private ConstSetting.ConfigSaveMode configSaveMode;

        public void InitSave()
        {
            ReadCurrentConfig();
        }

        public void OnDestroy()
        {

        }

        public void SaveData()
        {

        }

        private void ReadCurrentConfig()
        {
            // 反射读取ConstSetting静态字段
            var type = typeof(ConstSetting);

            // Resolution
            var resField = type.GetField("Resolution", BindingFlags.Static | BindingFlags.Public);
            if (resField != null)
            {
                int[] arr = resField.GetValue(null) as int[];
                resolution = arr != null ? new List<int>(arr) : new List<int>();
            }

            // DefaultResolution
            var defResField = type.GetField("DefaultResolution", BindingFlags.Static | BindingFlags.Public);
            if (defResField != null)
                defaultResolution = (Vector2Int)defResField.GetValue(null);

            // DefaultUISize
            var defUISizeField = type.GetField("DefaultUISize", BindingFlags.Static | BindingFlags.Public);
            if (defUISizeField != null)
                defaultUISize = (Vector2Int)defUISizeField.GetValue(null);

            // FileEncryptionKey
            var fileKeyField = type.GetField("FileEncryptionKey", BindingFlags.Static | BindingFlags.Public);
            if (fileKeyField != null)
                fileEncryptionKey = (string)fileKeyField.GetValue(null);

            // DefaultLanguage
            var defLangField = type.GetField("DefaultLanguage", BindingFlags.Static | BindingFlags.Public);
            if (defLangField != null)
                defaultLanguage = (Language)defLangField.GetValue(null);

            // LocalizationStringTable
            var locStrTableField = type.GetField("LocalizationStringTable", BindingFlags.Static | BindingFlags.Public);
            if (locStrTableField != null)
                localizationStringTable = (string)locStrTableField.GetValue(null);

            // LocalizationAssetTable
            var locAssetTableField = type.GetField("LocalizationAssetTable", BindingFlags.Static | BindingFlags.Public);
            if (locAssetTableField != null)
                localizationAssetTable = (string)locAssetTableField.GetValue(null);

            // LanguageFont
            var langFontField = type.GetField("LanguageFont", BindingFlags.Static | BindingFlags.Public);
            if (langFontField != null)
            {
                var dict = langFontField.GetValue(null) as Dictionary<Language, string>;
                languageFont = dict != null ? new Dictionary<Language, string>(dict) : new Dictionary<Language, string>();
            }

            // MillionInt
            var millionIntField = type.GetField("MillionInt", BindingFlags.Static | BindingFlags.Public);
            if (millionIntField != null)
                millionInt = (int)millionIntField.GetValue(null);

            // MillionLong
            var millionLongField = type.GetField("MillionLong", BindingFlags.Static | BindingFlags.Public);
            if (millionLongField != null)
                millionLong = (long)millionLongField.GetValue(null);

            // ConfigConfigSaveMode
            var configSaveModeField = type.GetField("ConfigConfigSaveMode", BindingFlags.Static | BindingFlags.Public);
            if (configSaveModeField != null)
                configSaveMode = (ConstSetting.ConfigSaveMode)configSaveModeField.GetValue(null);
        }

        public void OnGUI(EditorWindow window)
        {
            if (GUILayout.Button("刷新当前配置"))
            {
                ReadCurrentConfig();
            }

            GUILayout.Label("分辨率列表", EditorStyles.boldLabel);
            for (int i = 0; i < resolution.Count; i++)
            {
                resolution[i] = EditorGUILayout.IntField($"分辨率 {i}", resolution[i]);
            }
            if (GUILayout.Button("添加分辨率")) resolution.Add(0);
            if (resolution.Count > 0 && GUILayout.Button("移除最后一个分辨率")) resolution.RemoveAt(resolution.Count - 1);

            defaultResolution = EditorGUILayout.Vector2IntField("默认分辨率", defaultResolution);
            defaultUISize = EditorGUILayout.Vector2IntField("UI画布尺寸", defaultUISize);
            fileEncryptionKey = EditorGUILayout.TextField("加密Key", fileEncryptionKey);
            defaultLanguage = (Language)EditorGUILayout.EnumPopup("默认语言", defaultLanguage);
            localizationStringTable = EditorGUILayout.TextField("本地化字符串表", localizationStringTable);
            localizationAssetTable = EditorGUILayout.TextField("本地化资源表", localizationAssetTable);

            GUILayout.Label("语言字体映射", EditorStyles.boldLabel);
            foreach (var lang in System.Enum.GetValues(typeof(Language)))
            {
                string font = languageFont.ContainsKey((Language)lang) ? languageFont[(Language)lang] : "";
                font = EditorGUILayout.TextField(lang.ToString(), font);
                languageFont[(Language)lang] = font;
            }

            millionInt = EditorGUILayout.IntField("万分比整数基数", millionInt);
            millionLong = EditorGUILayout.LongField("万分比长整数基数", millionLong);
            configSaveMode = (ConstSetting.ConfigSaveMode)EditorGUILayout.EnumPopup("配置保存方式", configSaveMode);

            GUILayout.Space(20);
            if (GUILayout.Button("生成配置脚本"))
            {
                GenerateConfigScript();
            }
        }

        private void GenerateConfigScript()
        {
            string path = EditorUtility.SaveFilePanel("保存配置脚本", Application.dataPath, "ConstSetting.cs", "cs");
            if (string.IsNullOrEmpty(path)) return;

            var writer = new PowerCellStudio.CsWriter();

            // using
            writer.WriteUsing("System.Collections.Generic", "System.IO", "UnityEngine");
            writer.Space();

            // namespace
            writer.WriteLine("namespace PowerCellStudio");
            writer.StartWriteBody();

            // class
            writer.WriteLine("public class ConstSetting");
            writer.StartWriteBody();

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 路径分隔符");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly char[] PathSeparator = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 分包配置文件夹");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly string BundleAssetConfigFolder = \"AssetBundleData\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 分包配置文件名");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly string BundleAssetConfigName = \"AssetBundleData.asset\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 设计分辨率范围");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly int[] Resolution = new[] {{ {string.Join(", ", resolution)} }};");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 默认分辨率");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly Vector2Int DefaultResolution = new Vector2Int({defaultResolution.x}, {defaultResolution.y});");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 设计UI画布尺寸");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly Vector2Int DefaultUISize = new Vector2Int({defaultUISize.x}, {defaultUISize.y});");

            writer.WriteLine($"public static readonly string FileEncryptionKey = \"{fileEncryptionKey}\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 默认语言");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly Language DefaultLanguage = Language.{defaultLanguage};");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 本地化字符串表");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly string LocalizationStringTable = \"{localizationStringTable}\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 本地化资源表");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly string LocalizationAssetTable = \"{localizationAssetTable}\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 本地化语言对应字体");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly Dictionary<Language, string> LanguageFont = new Dictionary<Language, string>()");
            writer.StartWriteBody();
            foreach (var kv in languageFont)
            {
                writer.WriteLine($"{{ Language.{kv.Key}, \"{kv.Value}\" }},");
            }
            writer.EndWriteBody();

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 万分比整数基数");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly int MillionInt = {millionInt};");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 万分比长整数基数");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly long MillionLong = {millionLong};");

            writer.WriteLine("public enum ConfigSaveMode { ScriptableObject, Json, Binary }");
            writer.WriteLine($"public static readonly ConfigSaveMode ConfigConfigSaveMode = ConfigSaveMode.{configSaveMode};");

            writer.EndWriteBody(); // end class
            writer.EndWriteBody(); // end namespace

            File.WriteAllText(path, writer.ToString(), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成成功", "配置脚本已生成！", "OK");
        }
    }
}