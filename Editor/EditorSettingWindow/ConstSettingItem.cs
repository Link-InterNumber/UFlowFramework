using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PowerCellStudio.Editor
{
    public class ConstSettingItem : IEditorSettingWindowItem
    {
        public string itemName => "Const Setting";

        // 可编辑字段
        private List<int> resolution = new List<int>();
        private ResolutionLv defaultResolutionLv;
        private Vector2Int defaultUISize;
        private string fileEncryptionKey;
        private Language defaultLanguage;
        private string localizationStringTable;
        private string localizationAssetTable;
        private Dictionary<Language, string> languageFont = new Dictionary<Language, string>();
        // private Dictionary<Language, string> languageTMPFont = new Dictionary<Language, string>();
        private ConstSetting.ConfigSaveMode configSaveMode;
        private Language[] _languages;

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
            var defResField = type.GetField("DefaultResolutionLv", BindingFlags.Static | BindingFlags.Public);
            if (defResField != null)
                defaultResolutionLv = (ResolutionLv)defResField.GetValue(null);

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
            _languages = System.Enum.GetValues(typeof(Language)) as Language[];
            var langFontField = type.GetField("LanguageFont", BindingFlags.Static | BindingFlags.Public);
            if (langFontField != null)
            {
                var dict = langFontField.GetValue(null) as Dictionary<Language, string>;
                languageFont = dict != null ? new Dictionary<Language, string>(dict) : new Dictionary<Language, string>();
            }
            // var langTMPFontField = type.GetField("LanguageTMPFont", BindingFlags.Static | BindingFlags.Public);
            // if (langTMPFontField != null)
            // {
            //     var dict = langTMPFontField.GetValue(null) as Dictionary<Language, string>;
            //     languageTMPFont = dict != null ? new Dictionary<Language, string>(dict) : new Dictionary<Language, string>();
            // }

            // ConfigConfigSaveMode
            var configSaveModeField = type.GetField("ConfigConfigSaveMode", BindingFlags.Static | BindingFlags.Public);
            if (configSaveModeField != null)
                configSaveMode = (ConstSetting.ConfigSaveMode)configSaveModeField.GetValue(null);
        }

        public void OnGUI(EditorWindow window)
        {
            if (GUILayout.Button("Refresh Settings"))
            {
                ReadCurrentConfig();
            }

            GUILayout.Label("Resolution List", EditorStyles.boldLabel);
            var resolutionLvCount = Enum.GetValues(typeof(ResolutionLv)).Length;
            for (int i = 0; i < resolution.Count; i++)
            {
                if (i < resolutionLvCount)
                {
                    resolution[i] = EditorGUILayout.IntField($"resolution {(ResolutionLv)i}", resolution[i]);
                }
                else
                {
                    resolution[i] = EditorGUILayout.IntField($"resolution {i}", resolution[i]);
                }
            }
            if (GUILayout.Button("Add Resolution")) resolution.Add(0);
            if (resolution.Count > 0 && GUILayout.Button("Remove Last")) resolution.RemoveAt(resolution.Count - 1);

            defaultResolutionLv = (ResolutionLv)EditorGUILayout.EnumPopup("Default Resolution Lv", defaultResolutionLv);
            defaultUISize = EditorGUILayout.Vector2IntField("UI Canvas Size", defaultUISize);
            fileEncryptionKey = EditorGUILayout.TextField("file Encryption Key", fileEncryptionKey);
            defaultLanguage = (Language)EditorGUILayout.EnumPopup("default Language", defaultLanguage);
            localizationStringTable = EditorGUILayout.TextField("Localization String Table", localizationStringTable);
            localizationAssetTable = EditorGUILayout.TextField("Localization Asset Table", localizationAssetTable);
            EditorGUILayout.Space();
            GUILayout.Label("Language Fonts Table", EditorStyles.boldLabel);
            foreach (var lang in _languages)
            {
                string font = languageFont.TryGetValue(lang, out var value) ? value : string.Empty;
                font = EditorGUILayout.TextField(lang.ToString(), font);
                languageFont[lang] = font;
            }
            // GUILayout.Label("Language TMP Fonts Table", EditorStyles.boldLabel);
            // foreach (var lang in _languages)
            // {
            //     string font = languageTMPFont.TryGetValue(lang, out var value) ? value : string.Empty;
            //     font = EditorGUILayout.TextField(lang.ToString(), font);
            //     languageTMPFont[lang] = font;
            // }

            configSaveMode = (ConstSetting.ConfigSaveMode)EditorGUILayout.EnumPopup("Config Save Mode", configSaveMode);

            GUILayout.Space(20);
            if (GUILayout.Button("Save Settings"))
            {
                GenerateConfigScript();
            }
        }

        private void GenerateConfigScript()
        {
            string path = "Assets/UFlowFramework/Define/ConstSetting.cs";
            // if (string.IsNullOrEmpty(path)) return;

            var writer = new CsWriter();

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
            writer.WriteLine("/// <para>Path separator characters</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly char[] PathSeparator = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 分包配置文件夹");
            writer.WriteLine("/// <para>Subpackage configuration folder</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly string BundleAssetConfigFolder = \"AssetBundleData\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 分包配置文件名");
            writer.WriteLine("/// <para>Subpackage configuration file name</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly string BundleAssetConfigName = \"AssetBundleData.asset\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 设计分辨率范围");
            writer.WriteLine("/// <para>Design resolution range</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly int[] Resolution = new[] {{ {string.Join(", ", resolution)} }};");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 默认分辨率");
            writer.WriteLine("/// <para>Default resolution</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly ResolutionLv DefaultResolutionLv = ResolutionLv.{defaultResolutionLv};");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 设计UI画布尺寸(宽,高)");
            writer.WriteLine("/// <para>Default UI canvas size (width, height)</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly Vector2Int DefaultUISize = new Vector2Int({defaultUISize.x}, {defaultUISize.y});");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 文件加密密钥");
            writer.WriteLine("/// <para>File encryption key</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly string FileEncryptionKey = \"{fileEncryptionKey}\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 默认语言");
            writer.WriteLine("/// <para>Default language</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly Language DefaultLanguage = Language.{defaultLanguage};");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 本地化字符串表");
            writer.WriteLine("/// <para>Localization string table</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly string LocalizationStringTable = \"{localizationStringTable}\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 本地化资源表");
            writer.WriteLine("/// <para>Localization asset table</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly string LocalizationAssetTable = \"{localizationAssetTable}\";");

            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 本地化语言对应字体");
            writer.WriteLine("/// <para>Font mapping for each localization language</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine("public static readonly Dictionary<Language, string> LanguageFont = new Dictionary<Language, string>()");
            writer.StartWriteBody();
            foreach (var kv in languageFont)
            {
                writer.WriteLine($"{{ Language.{kv.Key}, \"{kv.Value}\" }},");
            }
            writer.EndWriteBody();
            writer.WriteLine(";");

            writer.WriteLine("public enum ConfigSaveMode { ScriptableObject, Json, Binary }");
            writer.WriteLine("/// <summary>");
            writer.WriteLine("/// 配置保存模式（默认二进制）");
            writer.WriteLine("/// <para>Configuration save mode (default: Binary)</para>");
            writer.WriteLine("/// </summary>");
            writer.WriteLine($"public static readonly ConfigSaveMode ConfigConfigSaveMode = ConfigSaveMode.{configSaveMode};");

            writer.EndWriteBody(); // end class
            writer.EndWriteBody(); // end namespace

            File.WriteAllText(path, writer.ToString(), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成成功", "配置脚本已生成！", "OK");
            EditorUtility.RevealInFinder(path);
        }
    }
}