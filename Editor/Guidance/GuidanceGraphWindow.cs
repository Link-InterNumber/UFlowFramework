using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio
{
    public class GuidanceGraphWindow : EditorWindow
    {
        [MenuItem("Tools/Guidance/Editor Graph")]
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
            _graphView = new GuidanceGraphView(this);
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(_graphView);
            _graphView.StretchToParentSize();

            var toolbarText = new Toolbar();
            var savePathField = new TextField("Save Path");
            savePathField.style.minWidth = 400;
            _currentSavePath = EditorSaveUtils.GetEditorPref("GuidanceGraphSavePath", "Assets/GuidanceGraphAsset");
            savePathField.value = _currentSavePath;
            savePathField.RegisterValueChangedCallback(evt =>
            {
                _currentSavePath = evt.newValue;
                EditorSaveUtils.SetEditorPref("GuidanceGraphSavePath", _currentSavePath);
            });
            toolbarText.Add(savePathField);

            var configIdField = new IntegerField("Config ID");
            configIdField.style.minWidth = 200;
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
            toolbarText.Add(configBtn);
            rootVisualElement.Add(toolbarText);

            var toolbar = new Toolbar();

            _assetObjectField = new ObjectField("Graph Asset")
            {
                objectType = typeof(GuidanceGraphAsset),
                allowSceneObjects = false
            };
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
                var assetName = EditorUtility.SaveFilePanelInProject("Sava Asset", "", "asset", "");
                AssetDatabase.CreateAsset(newAsset, assetName);
                _currentAsset = newAsset;
                _assetObjectField.value = _currentAsset;
            })
            { text = "Create" };
            toolbar.Add(createBtn);

            var saveBtn = new Button(() =>
            {
                var handlerTypes = ReflectionUtils.GetInstantiableSubclasses(typeof(IGuidanceGraphWriteHandler), typeof(GuidanceGraphWindow).Assembly);
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
            toolbar.Add(saveBtn);
            rootVisualElement.Add(toolbar);
        }
    }
}