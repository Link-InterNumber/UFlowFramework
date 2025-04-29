#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using Color = UnityEngine.Color;

namespace PowerCellStudio
{
    public class TextureFormatSetterItem : IEditorSettingWindowItem
    {

        private static GUIStyle guiStyle = new GUIStyle(EditorStyles.popup)
        {
            fontSize = 15,
            fixedHeight = 25,
            fixedWidth = 200,
        };
        private static string[] _DEFAULT_FORMAT = new string[1]{"Automatic"};


        //保存当前设置的格式
        private int curTFIndex = 0, setTFIndex;
        private TextureFormatSetterSize curSize = TextureFormatSetterSize.x2048, setSize;
        //保存当前设置的平台
        private EPlatform curPl, setPl;
        private bool autoOptimize;
        private bool autoSize;

        private string _printResult;
        private Dictionary<string, string[]> _textureFormatMapping;
        
        //<格式， List<路径>>
        private Dictionary<string, List<string>> _allSettings;
        private Vector2 _scrollPosition;


        public string itemName => "Set Texture Format";

        public void InitSave()
        {
            _textureFormatMapping = new Dictionary<string, string[]>();

            foreach (var keyValuePair in TextureFormatMapping.PlatformFormats)
            {
                _textureFormatMapping.Add(keyValuePair.Key, keyValuePair.Value.Select(o => o.ToString()).ToArray());
            } 
        }

        public void OnDestroy()
        {
            _printResult = null;
            _textureFormatMapping = null;
            _allSettings = null;
        }

        public void OnGUI()
        {
            if (Selection.assetGUIDs.Length <= 0)
            {
                GUILayout.Label("请先选择一个文件夹!!! ");
            }
            else
            {
                var folder = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (!Directory.Exists(folder))
                {
                    GUILayout.Label("请先选择一个文件夹!!! ");
                    return;
                }
                GUILayout.Label($"当前选中的文件夹：{folder}");
            }

            GUILayout.Space(10);

            GUILayout.Label("设置平台： ");
            setPl = (EPlatform) EditorGUILayout.EnumPopup(curPl, guiStyle);
            if (setPl != curPl)
            {
                curTFIndex = 0;
            }
            GUILayout.Space(10);

            GUILayout.Label("设置格式： ");
            string[] canSetFormats = null;
            if (!_textureFormatMapping.TryGetValue(setPl.ToString(), out canSetFormats))
            {
                canSetFormats = _DEFAULT_FORMAT;
            }
            setTFIndex = EditorGUILayout.Popup(curTFIndex, canSetFormats, guiStyle);

            // isConvertRGBA = EditorGUILayout.ToggleLeft("是否将RGB强制转成RGBA", isConvertRGBA);
            GUILayout.Space(10);
            
            GUILayout.Label("设置最大尺寸： ");
            setSize = (TextureFormatSetterSize) EditorGUILayout.EnumPopup(curSize, guiStyle);
            // isConvertRGBA = EditorGUILayout.ToggleLeft("是否将RGB强制转成RGBA", isConvertRGBA);
            GUILayout.Space(10);
            autoOptimize = EditorGUILayout.ToggleLeft("自动优化", autoOptimize);
            autoSize = EditorGUILayout.ToggleLeft("自动尺寸", autoSize);

            PrintSetting();

            if (GUILayout.Button("开始设置"))
            {
                _printResult = null;
                if (!CheckSelection())
                    return;

                ParseTexture2DFormat(true);
                AssetDatabase.SaveAssets();
                Debug.Log("完成");
            }

            GUILayout.Label("");
            if (GUILayout.Button("获取当前文件夹下所有文件当前平台的压缩格式"))
            {
                if (!CheckSelection())
                    return;

                _allSettings = new Dictionary<string, List<string>>();
                ParseTexture2DFormat(false);

                //打印日志
                StringBuilder sb = new StringBuilder();
                foreach (var dic in _allSettings)
                {
                    sb.Append($"类型<color=#FF722F>{dic.Key}</color>累计{dic.Value.Count}个:\n");
                    foreach (var path in dic.Value)
                    {
                        sb.Append($"\t{path}\n");
                    }
                    sb.Append($"\n");
                }
                _printResult = sb.ToString();
                _allSettings.Clear();
                _allSettings = null;
                sb.Clear();
                sb = null;
            }

            if (!string.IsNullOrEmpty(_printResult))
            {
                _scrollPosition =  GUILayout.BeginScrollView(_scrollPosition);
                GUILayout.Label(_printResult, new GUIStyle(){richText = true, normal = new GUIStyleState{textColor = Color.white}});
                GUILayout.EndScrollView();
            }
        }

        public void SaveData(){}

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
                if (isFix)
                {
                    var platform = curPl.ToString();
                    var textureFormat = TextureImporterFormat.Automatic;
                    if (TextureFormatMapping.PlatformFormats.TryGetValue(platform, out var formats))
                    {
                        textureFormat = formats[Mathf.Clamp(setTFIndex, 0, formats.Length - 1)];
                    }
                    TextureFormatSetter.SetPicFormat(path, platform, textureFormat, (int) curSize, autoOptimize, autoSize);
                }
                else
                    PrintPicFormat(path);
            }
        }

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
