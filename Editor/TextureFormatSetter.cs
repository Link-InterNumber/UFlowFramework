#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using Color = UnityEngine.Color;

namespace PowerCellStudio.Editor
{
    public enum EPlatform
    {
        DefaultTexturePlatform,
        Android,
        WebGL,
        StandaloneWindows,
    }

    public enum TextureFormatSetterSize
    {
        x32 = 32,
        x64 = 64,
        x128 = 128,
        x256 = 256,
        x512 = 512,
        x1024 = 1024,
        x2048 = 2048,
        x4096 = 4096,
        x8192 = 8192,
        x16384 = 16384,
    }

    public class TextureFormatSetter : EditorWindow
    {
        //保存当前设置的格式
        public int curTFIndex = 0, setTFIndex;
        public GUIStyle guiStyle;

        public TextureFormatSetterSize curSize = TextureFormatSetterSize.x2048, setSize;

        //保存当前设置的平台
        public EPlatform curPl, setPl;

        //是否强制设置成RGBA, false : 使用原有的RGB
        // public bool isConvertRGBA = true;

        public bool autoOptimize;
        public bool autoSize;

        // //是否是一个文件
        // public bool _isFile;
        private string _printResult;
        private Dictionary<string, string[]> _textureFormatMapping;

        private static string[] _DEFAULT_FORMAT = new string[1]{"Automatic"};

        public void Awake()
        {
            minSize = new Vector2(560f, 440f);
            //设置绘制下拉框的格式
            guiStyle = new GUIStyle(EditorStyles.popup);
            guiStyle.fixedHeight = 22;

            _textureFormatMapping = new Dictionary<string, string[]>();

            foreach (var keyValuePair in TextureFormatMapping.PlatformFormats)
            {
                _textureFormatMapping.Add(keyValuePair.Key, keyValuePair.Value.Select(o => o.ToString()).ToArray());
            } 
        }

        public void OnDestroy()
        {
            guiStyle = null;
            _printResult = null;
            _textureFormatMapping = null;
        }

        void OnGUI()
        {
            EditorUIStyle.DrawWindowBackground(new Rect(Vector2.zero, position.size));
            EditorUIStyle.DrawImguiHeader("Texture Format Setter",
                "Apply platform compression and size settings to every texture in the selected Project folder.");

            string selectedFolder = null;
            if (Selection.assetGUIDs.Length <= 0)
            {
                EditorGUILayout.HelpBox("请先在 Project 窗口中选择一个文件夹。", MessageType.Warning);
            }
            else
            {
                selectedFolder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (!Directory.Exists(selectedFolder))
                {
                    EditorGUILayout.HelpBox("当前选择不是有效文件夹。", MessageType.Warning);
                    selectedFolder = null;
                }
                else
                {
                    using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
                    {
                        EditorGUILayout.LabelField("Selected Folder", EditorUIStyle.SectionTitle);
                        EditorGUILayout.SelectableLabel(selectedFolder, EditorStyles.textField,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                }
            }

            GUILayout.Space(EditorUIStyle.ContentSpacing);
            using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
            {
                EditorGUILayout.LabelField("Import Settings", EditorUIStyle.SectionTitle);
                setPl = (EPlatform)EditorGUILayout.EnumPopup("Platform", curPl, guiStyle);
                if (setPl != curPl)
                    curTFIndex = 0;

                if (!_textureFormatMapping.TryGetValue(setPl.ToString(), out var canSetFormats))
                    canSetFormats = _DEFAULT_FORMAT;
                setTFIndex = EditorGUILayout.Popup("Format", curTFIndex, canSetFormats, guiStyle);

                setSize = (TextureFormatSetterSize)EditorGUILayout.EnumPopup("Maximum Size", curSize, guiStyle);
                EditorUIStyle.DrawSeparator();
                autoOptimize = EditorGUILayout.ToggleLeft("自动优化压缩格式", autoOptimize);
                autoSize = EditorGUILayout.ToggleLeft("根据源纹理自动选择尺寸", autoSize);
            }

            PrintSetting();

            GUILayout.Space(EditorUIStyle.ContentSpacing);
            using (new EditorGUI.DisabledScope(selectedFolder == null))
            {
                if (GUILayout.Button("应用纹理设置", EditorUIStyle.PrimaryButton))
                {
                    _printResult = null;
                    if (!CheckSelection())
                        return;

                    ParseTexture2DFormat(true);
                    AssetDatabase.SaveAssets();
                    Debug.Log("完成");
                }

                GUILayout.Space(EditorUIStyle.SectionSpacing);
                if (GUILayout.Button("扫描当前平台压缩格式", GUILayout.Height(EditorUIStyle.SecondaryButtonHeight)))
                {
                    if (!CheckSelection())
                        return;

                    _allSettings = new Dictionary<string, List<string>>();
                    ParseTexture2DFormat(false);

                    StringBuilder sb = new StringBuilder();
                    foreach (var dic in _allSettings)
                    {
                        sb.Append($"类型<color=#FF722F>{dic.Key}</color>累计{dic.Value.Count}个:\n");
                        foreach (var path in dic.Value)
                            sb.Append($"\t{path}\n");
                        sb.Append("\n");
                    }
                    _printResult = sb.ToString();
                    _allSettings.Clear();
                    _allSettings = null;
                }
            }

            if (!string.IsNullOrEmpty(_printResult))
            {
                GUILayout.Space(EditorUIStyle.ContentSpacing);
                using (new EditorGUILayout.VerticalScope(EditorUIStyle.PanelBox))
                {
                    EditorGUILayout.LabelField("Scan Result", EditorUIStyle.SectionTitle);
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    GUILayout.Label(_printResult, EditorUIStyle.RichTextLabel);
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        public static void SetTextureFormat()
        {
            EditorWindow.GetWindow<TextureFormatSetter>(false, "设置图片压缩格式", true).Show();
        }

        //@isFix: 是否修改（false ： 只打印信息）
        public void ParseTexture2DFormat(bool isFix)
        {
            var folder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            if (!Directory.Exists(folder)) return;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] {folder});
            foreach (var uid in guids)
            {
                string directoryPath = AssetDatabase.GUIDToAssetPath(uid);
                ParseTexture(directoryPath, isFix);
            }
        }

        private void ParseTexture(string path, bool isFix)
        {
            if (path.EndsWith(".png") || path.EndsWith(".jpg") ||
                path.EndsWith(".tga") || path.EndsWith(".psd") ||
                path.EndsWith(".PSD") || path.EndsWith(".exr") ||
                path.EndsWith(".tif"))
            {
                // Debug.Log("-------------path: " + path);
                // Debug.Log(Application.dataPath);
                // Debug.Log(Application.dataPath.Replace("Assets",""));
                // string parseName = path.Replace('\\', '/')
                //     .Replace(Application.dataPath.Replace("Assets", ""), "");
                if (isFix)
                {
                    var platform = curPl.ToString();
                    var textureFormat = TextureImporterFormat.Automatic;
                    if (TextureFormatMapping.PlatformFormats.TryGetValue(platform, out var formats))
                    {
                        textureFormat = formats[Mathf.Clamp(setTFIndex, 0, formats.Length - 1)];
                    }
                    SetPicFormat(path, platform, textureFormat, (int) curSize, autoOptimize, autoSize);
                }
                else
                    PrintPicFormat(path);
            }
        }

        private static bool IsTextureCanCrunch(int width, int height)
        {
            return width % 4 == 0 && height % 4 == 0;
        }
        
        private static bool IsPOTTexture(int width, int height)
        {
            return formatSize.Contains(width) && formatSize.Contains(height);
        }

        //设置图片格式
        public static void SetPicFormat(string path, string curPlatform, TextureImporterFormat curFormat, int maxSize, bool autoOptimize, bool autoSize)
        {
            if(!Application.isEditor) return;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer == null || !importer) return;
            Texture2D texture = null;
            if (autoOptimize)
            {
                try
                {
                    var fileData = File.ReadAllBytes(path);
                    texture = new Texture2D(0, 0);
                    texture.LoadImage(fileData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"texture == null, Path = [{path}]");
                    return;
                }
                int textureMaxSize = Mathf.Max(texture.height, texture.width);
                int fitSize = autoSize ?  FitSize(textureMaxSize) : maxSize;
                var finalSize = new Vector2Int(texture.width, texture.height);
                if (finalSize.x > fitSize && finalSize.x > finalSize.y)
                {
                    finalSize.x = fitSize;
                    finalSize.y = Mathf.RoundToInt((float) fitSize / finalSize.x * finalSize.y);
                }
                else if (finalSize.y > fitSize && finalSize.y > finalSize.x)
                {
                    finalSize.y = fitSize;
                    finalSize.x = Mathf.RoundToInt((float) fitSize / finalSize.y * finalSize.x);
                }
                bool isPOT = IsPOTTexture(finalSize.x, finalSize.y);
                bool isCanCrunch = IsTextureCanCrunch(finalSize.x, finalSize.y);
                if (isPOT)
                {
                    var androidSetting = importer.GetPlatformTextureSettings(curPlatform);
                    androidSetting.overridden = false;
                    var defaultSetting = importer.GetPlatformTextureSettings(EPlatform.DefaultTexturePlatform.ToString());
                    defaultSetting.maxTextureSize = fitSize;
                    defaultSetting.format = TextureImporterFormat.Automatic;
                    defaultSetting.crunchedCompression = true;
                    defaultSetting.compressionQuality = 50;
                    importer.SetPlatformTextureSettings(androidSetting);
                    importer.SetPlatformTextureSettings(defaultSetting);
                }
                else if (isCanCrunch)
                {
                    var androidSetting = importer.GetPlatformTextureSettings(curPlatform);
                    androidSetting.overridden = true;
                    androidSetting.maxTextureSize = fitSize;
                    androidSetting.format = TextureImporterFormat.DXT5Crunched;
                    importer.SetPlatformTextureSettings(androidSetting);
                }
                else
                {
                    var androidSetting = importer.GetPlatformTextureSettings(curPlatform);
                    androidSetting.overridden = true;
                    androidSetting.maxTextureSize = fitSize;
                    androidSetting.format = curFormat;
                    importer.SetPlatformTextureSettings(androidSetting);
                }
                AssetDatabase.ImportAsset(path);
                return;
            }
            var setting = importer.GetPlatformTextureSettings(curPlatform);
            setting.overridden = !curPlatform.Equals(EPlatform.DefaultTexturePlatform.ToString());
            if (autoSize)
            {
                try
                {
                    var fileData = File.ReadAllBytes(path);
                    texture = new Texture2D(0, 0);
                    texture.LoadImage(fileData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"texture == null, Path = [{path}]");
                    return;
                }
                int textureMaxSize = Mathf.Max(texture.height, texture.width);
                int fitSize = FitSize(textureMaxSize);
                setting.maxTextureSize = fitSize;
            } 
            else
            {
                setting.maxTextureSize = maxSize;
            }
            setting.format = curFormat;
            importer.SetPlatformTextureSettings(setting);
            AssetDatabase.ImportAsset(path);
            Debug.LogFormat("<<<Set path{0}, format: {1}: ", path, setting.format);
        }

        //<格式， List<路径>>
        private Dictionary<string, List<string>> _allSettings;

        private void PrintPicFormat(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer == null || !importer) return;
            var setting = importer.GetPlatformTextureSettings(curPl.ToString());
            string formatName = setting.format.ToString();
            // Debug.LogFormat("path: {0}, format：{1}", path, formatName);

            if (_allSettings.ContainsKey(formatName))
                _allSettings[formatName].Add(path);
            else
            {
                _allSettings[formatName] = new List<string>();
                _allSettings[formatName].Add(path);
            }
        }

        static int[] formatSize = new int[] {32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384};
        private Vector2 _scrollPosition;

        private static int FitSize(int picValue)
        {
            foreach (var one in formatSize)
            {
                if (picValue <= one)
                {
                    return one;
                }
            }

            return 2048;
        }

        private static bool CheckSelection()
        {
            if (Selection.assetGUIDs.Length <= 0)
            {
                Debug.LogError("请先选择一个文件夹!!! ");
                return false;
            }
            var folder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            if (!Directory.Exists(folder))
            {
                Debug.LogError("请先选择一个文件夹!!! ");
                return false;
            }

            return true;
        }

        private void PrintSetting()
        {
            if (setPl != curPl)
            {
                curPl = setPl;
                Debug.LogError("当前平台：" + curPl);
            }

            if (setTFIndex != curTFIndex)
            {
                curTFIndex = setTFIndex;
                Debug.LogError("当前格式" + curTFIndex);
            }
            
            if (setSize != curSize)
            {
                curSize = setSize;
                Debug.LogError("当前最大尺寸" + curSize);
            }
        }
    }
}
#endif
