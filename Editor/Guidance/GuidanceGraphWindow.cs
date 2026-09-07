using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    public class GuidanceGraphWindow : EditorWindow
    {
        public static void OpenWindow()
        {
            GuidanceGraphWindow window = GetWindow<GuidanceGraphWindow>();
            window.titleContent = new GUIContent("Guidance Graph");
        }

        private string _currentSavePath = "Assets/GuidanceGraphAsset";
        private GuidanceGraphView _graphView;
        private GuidanceGraphAsset _currentAsset;
        private ObjectField _assetObjectField;
        private int _currentConfigId = 0;

        private void OnEnable()
        {
            minSize = new Vector2(760f, 480f);
            rootVisualElement.Clear();
            EditorUIStyle.ApplyRoot(rootVisualElement);

            rootVisualElement.Add(EditorUIStyle.CreateHeader("Guidance Graph",
                "Build guidance sequences from configuration data or reusable graph assets."));

            _graphView = new GuidanceGraphView(this);
            _graphView.style.flexGrow = 1f;
            _graphView.style.minHeight = 260f;

            var toolbarText = new Toolbar();
            EditorUIStyle.ApplyToolbar(toolbarText, EditorUIStyle.SectionSpacing);
            var savePathField = new TextField("Save Path");
            savePathField.style.minWidth = 280f;
            savePathField.style.flexGrow = 1f;
            _currentSavePath = EditorSaveUtils.GetEditorPref("GuidanceGraphSavePath", "Assets/GuidanceGraphAsset");
            savePathField.value = _currentSavePath;
            savePathField.RegisterValueChangedCallback(evt =>
            {
                _currentSavePath = evt.newValue;
                EditorSaveUtils.SetEditorPref("GuidanceGraphSavePath", _currentSavePath);
            });
            toolbarText.Add(savePathField);

            var configIdField = new IntegerField("Config ID");
            configIdField.style.width = 150f;
            configIdField.value = _currentConfigId;
            configIdField.RegisterValueChangedCallback(evt =>
            {
                _currentConfigId = evt.newValue;
            });
            toolbarText.Add(configIdField);
            var configBtn = new Button(() =>
            {
                _graphView.ReadFromConfigs(_currentConfigId);
            })
            { text = "Read Config" };
            ConfigureToolbarButton(configBtn);
            toolbarText.Add(configBtn);
            rootVisualElement.Add(toolbarText);

            var toolbar = new Toolbar();
            EditorUIStyle.ApplyToolbar(toolbar, EditorUIStyle.ContentSpacing);

            _assetObjectField = new ObjectField("Graph Asset")
            {
                objectType = typeof(GuidanceGraphAsset),
                allowSceneObjects = false
            };
            _assetObjectField.style.minWidth = 280f;
            _assetObjectField.style.flexGrow = 1f;
            _assetObjectField.RegisterValueChangedCallback(evt =>
            {
                var asset = evt.newValue as GuidanceGraphAsset;
                if (_currentAsset == asset)
                    return;
                if (asset != null)
                {
                    _currentAsset = asset;
                    _graphView.ReadFromAsset(asset);
                }
                else
                {
                    _currentAsset = null;
                    _graphView.ClearGraph();
                }
            });
            toolbar.Add(_assetObjectField);

            var createBtn = new Button(() =>
            {
                var newAsset = ScriptableObject.CreateInstance<GuidanceGraphAsset>();
                var assetName = EditorUtility.SaveFilePanelInProject("Save Guidance Graph Asset", "GuidanceGraphAsset", "asset", "");
                if (string.IsNullOrEmpty(assetName))
                {
                    DestroyImmediate(newAsset);
                    return;
                }
                AssetDatabase.CreateAsset(newAsset, assetName);
                _currentAsset = newAsset;
                _assetObjectField.value = _currentAsset;
            })
            { text = "Create" };
            ConfigureToolbarButton(createBtn);
            toolbar.Add(createBtn);

            var saveBtn = new Button(() =>
            {
                var handlerTypes = ReflectionUtils.GetInstantiableSubtype(typeof(IGuidanceGraphWriteHandler), typeof(GuidanceGraphWindow).Assembly);
                if (handlerTypes.Count == 0)
                {
                    Debug.LogError("No GuidanceGraphWriteHandler found.");
                    return;
                }
                var handlerType = handlerTypes[0];
                var handler = ReflectionUtils.CreateInstance(handlerType) as IGuidanceGraphWriteHandler;
                if (handler == null)
                {
                    Debug.LogError("Failed to create GuidanceGraphWriteHandler instance.");
                    return;
                }
                var setupSuccess = handler.SetUp();
                if (!setupSuccess)
                {
                    Debug.LogWarning("Guidance graph save operation was cancelled or failed during setup.");
                    return;
                }

                if (!Directory.Exists(_currentSavePath))
                {
                    var isOk = EditorUtility.DisplayDialog("Save Error", "The specified save path does not exist.", "OK", "Cancel");
                    if (isOk)
                    {
                        Directory.CreateDirectory(_currentSavePath);
                        EditorSaveUtils.SetEditorPref("GuidanceGraphSavePath", _currentSavePath);
                    }
                    else
                        return;
                }
                // var savePath = Path.Combine(_currentSavePath, _currentAsset.name);
                _graphView.WriteAsset(ref _currentAsset, in handler, _currentSavePath);
                handler.SetDown();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            })
            { text = "Save" };
            ConfigurePrimaryButton(saveBtn);
            toolbar.Add(saveBtn);
            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(_graphView);
        }

        private static void ConfigureToolbarButton(Button button)
        {
            button.style.minWidth = 80f;
            button.style.height = 22f;
            button.style.marginLeft = 4f;
        }

        private static void ConfigurePrimaryButton(Button button)
        {
            ConfigureToolbarButton(button);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.backgroundColor = EditorUIStyle.AccentColor;
            button.style.color = Color.white;
        }
    }
}