using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerCellStudio.Editor
{
    public class GuidanceNodeView : Node
    {
        private GuidanceGraphView _owner;

        private GameObject _windowPrefab;
        private GameObject _tagTarget;

        private int _guidanceId;
        private string _guidanceDecs;
        private bool _touchScreenToSkip;
        private bool _blockInteraction;
        private GameObject _uiPrefab;

        public int GetGuidanceId()
        {
            return _guidanceId;
        }

        public string GetDecs() => _guidanceDecs;

        public bool GetTouchScreenToSkip() => _touchScreenToSkip;

        public bool GetBlockInteraction() => _blockInteraction;

        public GameObject GetUiPrefab() => _uiPrefab;

        public string GetPrefabGuid()
        {
            return _windowPrefab ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_windowPrefab)) : string.Empty;
        }

        public string GetTargetNodePath()
        {
            if (_tagTarget == null || _windowPrefab == null)
                return string.Empty;
            var prefabRoot = _windowPrefab.transform;
            var targetTransform = _tagTarget.transform;
            var pathBuilder = new StringBuilder();
            while (targetTransform != null && targetTransform != prefabRoot)
            {
                pathBuilder.Insert(0, targetTransform.name);
                targetTransform = targetTransform.parent;
                if (targetTransform != null && targetTransform != prefabRoot)
                {
                    pathBuilder.Insert(0, '/');
                }
            }
            return pathBuilder.ToString();
        }

        public GuidanceNodeView(int guidanceId, string guid, string targetPath, GuidanceGraphView owner)
        {
            _owner = owner;
            _guidanceId = guidanceId;
            _windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            // 通过targetPath找到_windowPrefab上的目标物体
            if (!string.IsNullOrEmpty(targetPath) && _windowPrefab)
            {
                var prefabRoot = _windowPrefab.transform;
                var pathSplits = targetPath.Split('/');
                Transform current = prefabRoot;
                for (int i = 1; i < pathSplits.Length; i++)
                {
                    current = current.Find(pathSplits[i]);
                    if (current == null)
                        break;
                    if (i == pathSplits.Length - 1)
                    {
                        _tagTarget = current.gameObject;
                    }
                }
            }
            Draw();
            RefreshView(_guidanceId);
            style.width = 320f;
            style.borderTopWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderTopColor = EditorUIStyle.ImguiBorderColor;
            style.borderBottomColor = EditorUIStyle.ImguiBorderColor;
            style.borderLeftColor = EditorUIStyle.ImguiBorderColor;
            style.borderRightColor = EditorUIStyle.ImguiBorderColor;
            titleContainer.style.backgroundColor = EditorUIStyle.SummaryBackground;
            mainContainer.style.backgroundColor = EditorUIStyle.PanelBackground;
        }

        private IntegerField _intField;
        private TextField _guidanceDecsField;
        private Toggle _touchSkipToggle;
        private Toggle _blockInteractionToggle;
        private ObjectField _handField;
        private ObjectField _windowField;
        private ObjectField _tagTargetField;
        private Label _exitingTag;
        private string _exitingTagStr = string.Empty;

        private void Draw()
        {
            _intField = new IntegerField();
            _intField.label = "Guidance Id";
            ConfigureField(_intField);
            _intField.RegisterValueChangedCallback(evt =>
            {
                _guidanceId = evt.newValue;
                RefreshView(_guidanceId);
            });
            mainContainer.Add(_intField);

            _guidanceDecsField = new TextField()
            {
                multiline = true
            };
            // 确保允许换行显示
            _guidanceDecsField.style.whiteSpace = WhiteSpace.Normal;
            _guidanceDecsField.style.minHeight = 58f;
            _guidanceDecsField.label = "Description";
            ConfigureField(_guidanceDecsField);

            _guidanceDecsField.RegisterValueChangedCallback(evt =>
            {
                _guidanceDecs = evt.newValue;
            });
            mainContainer.Add(_guidanceDecsField);

            _touchSkipToggle = new Toggle("Touch Screen To Skip");
            ConfigureField(_touchSkipToggle);

            _touchSkipToggle.RegisterValueChangedCallback(evt =>
            {
                _touchScreenToSkip = evt.newValue;
            });
            mainContainer.Add(_touchSkipToggle);

            _blockInteractionToggle = new Toggle("Block Interaction");
            ConfigureField(_blockInteractionToggle);

            _blockInteractionToggle.RegisterValueChangedCallback(evt =>
            {
                _blockInteraction = evt.newValue;
            });
            mainContainer.Add(_blockInteractionToggle);

            _handField = new ObjectField();
            _handField.objectType = typeof(UnityEngine.GameObject);
            _handField.allowSceneObjects = false;
            _handField.label = "Hand";
            ConfigureField(_handField);

            _handField.RegisterValueChangedCallback(evt =>
            {
                _uiPrefab = evt.newValue as GameObject;
            });
            mainContainer.Add(_handField);

            _windowField = new ObjectField();
            _windowField.objectType = typeof(UnityEngine.GameObject);
            _windowField.allowSceneObjects = false;
            _windowField.label = "Window";
            ConfigureField(_windowField);

            _windowField.RegisterValueChangedCallback(evt =>
            {
                _windowPrefab = evt.newValue as GameObject;
                _tagTarget = null;
                RefreshView(_guidanceId);
            });
            mainContainer.Add(_windowField);

            var openButton = new Button(() =>
            {
                if (_windowPrefab)
                    AssetDatabase.OpenAsset(_windowPrefab);
            })
            { text = "Open Prefab" };
            openButton.style.height = 24f;
            openButton.style.marginTop = 3f;
            openButton.style.marginBottom = 6f;
            mainContainer.Add(openButton);

            _tagTargetField = new ObjectField();
            _tagTargetField.objectType = typeof(UnityEngine.GameObject);
            _tagTargetField.allowSceneObjects = true;
            _tagTargetField.label = "Target Node";
            ConfigureField(_tagTargetField);

            _tagTargetField.RegisterValueChangedCallback(evt =>
            {
                _tagTarget = evt.newValue as GameObject;
                RefreshView(_guidanceId);
            });
            mainContainer.Add(_tagTargetField);

            _exitingTag = new Label();
            _exitingTag.name = "ExitingTag";
            _exitingTag.style.marginTop = 4f;
            _exitingTag.style.paddingLeft = 6f;
            _exitingTag.style.paddingRight = 6f;
            _exitingTag.style.paddingTop = 4f;
            _exitingTag.style.paddingBottom = 4f;
            _exitingTag.style.whiteSpace = WhiteSpace.Normal;
            _exitingTag.style.color = EditorUIStyle.MutedTextColor;
            _exitingTag.style.backgroundColor = EditorUIStyle.SummaryBackground;
            mainContainer.Add(_exitingTag);

            // 端口
            var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            inputPort.portName = "Previous";
            inputPort.portColor = EditorUIStyle.AccentHoverColor;
            inputContainer.Add(inputPort);

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "Next";
            outputPort.portColor = new Color(1f, 0.58f, 0.3f, 1f);
            outputContainer.Add(outputPort);
        }

        private static void ConfigureField(VisualElement field)
        {
            field.style.marginTop = 2f;
            field.style.marginBottom = 2f;
        }

        private void RefreshView(int guidanceId)
        {
            _guidanceId = guidanceId;
            title = guidanceId > 0 ? guidanceId.ToString() : "New Guidance Node";
            if (_guidanceId > 0)
            {
                var config = _owner.confProvider(_guidanceId);
                _guidanceDecs = config?.decs.rawString;
                _touchScreenToSkip = config?.touchScreenToSkip ?? false;
                _blockInteraction = config?.blockInteraction ?? false;
                _uiPrefab = config?.uiPrefab.assetName != null
                    ? AssetDatabase.LoadAssetAtPath<GameObject>(config?.uiPrefab.assetName)
                    : null;
            }

            if (_tagTarget)
            {
                var tags = _tagTarget.GetComponents<GuidanceTag>();
                var sb = new StringBuilder();
                for (int i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    sb.AppendLine($"Tag #{i + 1}: {tag.guidanceIndex}");
                }
                if (sb.Length > 0)
                    _exitingTagStr = sb.ToString();
                else
                    _exitingTagStr = "No Exiting Tag Target Set.";
            }
            else
            {
                _exitingTagStr = "No Exiting Tag Target Set.";
            }

            _intField.value = _guidanceId;
            _guidanceDecsField.value = _guidanceDecs;
            _touchSkipToggle.value = _touchScreenToSkip;
            _blockInteractionToggle.value = _blockInteraction;
            _handField.value = _uiPrefab;
            _windowField.value = _windowPrefab;
            _tagTargetField.value = _tagTarget;
            _exitingTag.text = _exitingTagStr;
            RefreshPorts();
            RefreshExpandedState();
        }

        public void AddTagToTarget()
        {
            if (_tagTarget == null)
                return;
            var existingTags = _tagTarget.GetComponents<GuidanceTagUI>();
            foreach (var tag in existingTags)
            {
                if (tag.guidanceIndex == _guidanceId)
                    return;
            }
            var newTag = _tagTarget.AddComponent<GuidanceTagUI>();
            newTag.guidanceIndex = _guidanceId;
        }
    }
}