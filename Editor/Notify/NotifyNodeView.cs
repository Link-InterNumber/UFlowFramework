using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace PowerCellStudio.Editor
{
    public class NotifyNodeView : Node
    {
        private static readonly Color DuplicateBackground = new Color(0.42f, 0.16f, 0.18f, 0.96f);
        private static readonly Color OutputPortColor = new Color(0.95f, 0.55f, 0.2f, 1f);

        private TextField _nodeNameField;
        private string _nodeName;
        private NotifyGraphView _owner;

        public NotifyNodeView(string name, NotifyGraphView owner)
        {
            _nodeName = name;
            title = _nodeName;
            _owner = owner;
            style.minWidth = 220f;
            style.width = 240f;
            style.backgroundColor = EditorUIStyle.PanelBackground;
            style.borderTopWidth = 1f;
            style.borderBottomWidth = 1f;
            style.borderLeftWidth = 1f;
            style.borderRightWidth = 1f;
            style.borderTopColor = EditorUIStyle.ImguiBorderColor;
            style.borderBottomColor = EditorUIStyle.ImguiBorderColor;
            style.borderLeftColor = EditorUIStyle.ImguiBorderColor;
            style.borderRightColor = EditorUIStyle.ImguiBorderColor;
            titleContainer.style.backgroundColor = _nodeName == "Root"
                ? EditorUIStyle.AccentColor
                : EditorUIStyle.SummaryBackground;
            mainContainer.style.paddingBottom = 5f;
            // Create input and output ports
            if (_nodeName != "Root")
            {
                _nodeNameField = new TextField("Type Name");
                _nodeNameField.style.marginLeft = 6f;
                _nodeNameField.style.marginRight = 6f;
                _nodeNameField.style.marginTop = 5f;
                _nodeNameField.style.marginBottom = 3f;
                _nodeNameField.value = _nodeName;
                _nodeNameField.RegisterValueChangedCallback(evt =>
                {
                    _nodeName = evt.newValue;
                    title = _nodeName;
                    _owner.CheckNodeDuplicate();
                });
                mainContainer.Add(_nodeNameField);
                var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
                inputPort.portName = "Parent";
                inputPort.portColor = EditorUIStyle.AccentColor;
                inputContainer.Add(inputPort);
            }

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputPort.portName = "Children";
            outputPort.portColor = OutputPortColor;
            outputContainer.Add(outputPort);

            // 刷新端口与展开状态
            RefreshPorts();
            RefreshExpandedState();
        }

        public string GetNodeName()
        {
            return _nodeName;
        }

        public void SetDuplicateState(bool isDuplicate)
        {
            style.backgroundColor = isDuplicate ? DuplicateBackground : EditorUIStyle.PanelBackground;
        }
    }
}