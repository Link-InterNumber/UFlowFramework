using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class NotifyNodeView : Node
    {
        private TextField nodeNameField;
        private string nodeName;
        private NotifyGraphView _owner;

        public NotifyNodeView(string name, NotifyGraphView owner)
        {
            nodeName = name;
            title = nodeName;
            _owner = owner;
            // Create input and output ports
            if (nodeName != "Root")
            {
                nodeNameField = new TextField("Type Name");
                nodeNameField.value = nodeName;
                nodeNameField.RegisterValueChangedCallback(evt =>
                {
                    nodeName = evt.newValue;
                    title = nodeName;
                    _owner.CheckNodeDuplicate();
                });
                mainContainer.Add(nodeNameField);

                var removeButton = new Button(() =>
                {
                    RemoveFromHierarchy();
                    _owner.CheckNodeDuplicate();
                }){ text = "Remove Node" };
                mainContainer.Add(removeButton);
                var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
                inputPort.portName = "Parent";
                inputPort.portColor = Color.green;
                inputContainer.Add(inputPort);
            }

            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputPort.portName = "Children";
            outputPort.portColor = Color.red;
            outputContainer.Add(outputPort);

            // 刷新端口与展开状态
            RefreshPorts();
            RefreshExpandedState();
        }

        public string GetNodeName()
        {
            return nodeName;
        }
    }
}