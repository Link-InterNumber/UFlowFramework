using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio.Editor
{
    public partial class AdvancedUICodeGeneratorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/UFlow/Advanced UI Code Generator";
        private const string NamespaceEditorPrefsKey = "PowerCellStudio.AdvancedUICodeGeneratorWindow.Namespace";
        private const string PrefabPathEditorPrefsKey = "PowerCellStudio.AdvancedUICodeGeneratorWindow.PrefabPath";
        private const string PendingBindInfoSessionStateKey = "PowerCellStudio.AdvancedUICodeGeneratorWindow.PendingBindInfo";

        private static readonly Type[] TargetComponentTypes =
        {
            typeof(Button),
            typeof(Toggle),
            typeof(Slider),
            typeof(InputField),
            typeof(IListUpdater),
        };

        private static readonly HashSet<Type> IgnoredComponentTypes = new HashSet<Type>
        {
            typeof(CanvasRenderer),
            typeof(Outline),
            typeof(Shadow),
            typeof(LayoutElement),
            typeof(ContentSizeFitter),
            typeof(Scrollbar),
        };

        private static readonly string[] NamePrefixes =
        {
            "btn", "button", "tgl", "toggle", "sld", "slider", "ipf", "lst", "list", "inputfield", "txt", "text", "img", "image"
        };

        private static readonly Dictionary<string, string> ComponentPrefixes = new Dictionary<string, string>
        {
            { "Button", "Btn" },
            { "Toggle", "Tgl" },
            { "Slider", "Sld" },
            { "InputField", "Ipf" },
            { "IListUpdater", "Lst" },
            { "RectTransform", "Rect" },
            { "Image", "Img" },
            { "Text", "Txt" },
            { "TextMeshProUGUI", "Txt"}
        };

        private GameObject _prefab;
        private string _prefabPath;
        private string _namespaceName = string.Empty;
        private string _className = string.Empty;
        private string _outputFolder = "Assets";
        private bool _generateVariableWindow;
        private bool _generateVirtualWindow;
        private Vector2 _scrollPosition;
        private List<NodeInfo> _nodes = new List<NodeInfo>();
        private List<ScriptFileInfo> _pendingScriptFiles;
        private PendingBindInfo _pendingBindInfo;

        public static void Open()
        {
            GetWindow<AdvancedUICodeGeneratorWindow>("UI Code Generator");
        }

        private void OnEnable()
        {
            _namespaceName = EditorPrefs.GetString(NamespaceEditorPrefsKey, string.Empty);

            if (TryRestoreCachedPrefab()) return;

            if (Selection.activeObject is GameObject selected && PrefabUtility.GetPrefabAssetType(selected) != PrefabAssetType.NotAPrefab)
            {
                SetPrefab(selected);
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawPrefabField();

            using (new EditorGUI.DisabledScope(_prefab == null))
            {
                DrawSettings();
                DrawGenerationOptions();

                EditorGUILayout.Space();
                DrawToolbar();
                EditorGUILayout.Space();
                DrawNodeTree();
                EditorGUILayout.Space();

                DrawActionButtons();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Advanced UI Script Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawPrefabField()
        {
            EditorGUI.BeginChangeCheck();
            var prefab = EditorGUILayout.ObjectField("UI Prefab", _prefab, typeof(GameObject), false) as GameObject;
            if (EditorGUI.EndChangeCheck() || (_prefab == null && prefab != null))
            {
                SetPrefab(prefab);
            }
        }

        private void DrawSettings()
        {
            EditorGUI.BeginChangeCheck();
            _namespaceName = EditorGUILayout.TextField("Namespace", _namespaceName);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(NamespaceEditorPrefsKey, _namespaceName);
            }

            _className = EditorGUILayout.TextField("UIWindow Class", _className);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                SelectOutputFolder();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGenerationOptions()
        {
            EditorGUI.BeginChangeCheck();
            _generateVariableWindow = EditorGUILayout.ToggleLeft("Generate UIVariableWindow script", _generateVariableWindow);
            if (EditorGUI.EndChangeCheck() && _generateVariableWindow)
            {
                _generateVirtualWindow = false;
            }

            using (new EditorGUI.DisabledScope(_generateVariableWindow))
            {
                _generateVirtualWindow = EditorGUILayout.ToggleLeft("Generate UIVirtualWindow script", _generateVirtualWindow);
            }

            if (_generateVariableWindow)
            {
                EditorGUILayout.HelpBox("UIVariableWindow mode only generates variable window and ctrl partial scripts.", MessageType.Info);
            }
        }

        private void DrawActionButtons()
        {
            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Generate Scripts", GUILayout.Height(30)))
                {
                    GenerateScripts();
                }
            }

            if (GUILayout.Button("Add/Bind UIWindow Script To Prefab", GUILayout.Height(26)))
            {
                TryBindCurrentPrefabComponent();
            }
        }

        private bool TryRestoreCachedPrefab()
        {
            var cachedPrefabPath = EditorPrefs.GetString(PrefabPathEditorPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(cachedPrefabPath)) return false;

            var cachedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cachedPrefabPath);
            if (cachedPrefab == null) return false;

            SetPrefab(cachedPrefab);
            return true;
        }

        private void SetPrefab(GameObject prefab)
        {
            _prefab = prefab;
            _prefabPath = _prefab ? AssetDatabase.GetAssetPath(_prefab) : string.Empty;
            _nodes.Clear();

            if (string.IsNullOrEmpty(_prefabPath))
                EditorPrefs.DeleteKey(PrefabPathEditorPrefsKey);
            else
                EditorPrefs.SetString(PrefabPathEditorPrefsKey, _prefabPath);

            if (_prefab == null)
            {
                _className = string.Empty;
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(_prefab) == PrefabAssetType.NotAPrefab)
            {
                EditorUtility.DisplayDialog("Invalid Prefab", "Please select a prefab asset.", "OK");
                _prefab = null;
                _prefabPath = string.Empty;
                EditorPrefs.DeleteKey(PrefabPathEditorPrefsKey);
                _className = string.Empty;
                return;
            }

            _className = MakeValidTypeName(_prefab.name.Replace(" ", string.Empty));
            var prefabFolder = Path.GetDirectoryName(_prefabPath);
            if (!string.IsNullOrEmpty(prefabFolder)) _outputFolder = prefabFolder.Replace("\\", "/");
            BuildNodeInfos();
        }

        private void SelectOutputFolder()
        {
            var startPath = string.IsNullOrEmpty(_outputFolder) ? Application.dataPath : Path.GetFullPath(_outputFolder);
            var selectedPath = EditorUtility.OpenFolderPanel("Select UI Script Output Folder", startPath, string.Empty);
            if (string.IsNullOrEmpty(selectedPath)) return;

            selectedPath = selectedPath.Replace("\\", "/");
            var projectPath = Directory.GetCurrentDirectory().Replace("\\", "/");
            if (!selectedPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Output folder must be inside this Unity project.", "OK");
                return;
            }

            _outputFolder = selectedPath.Substring(projectPath.Length + 1);
        }
    }
}
