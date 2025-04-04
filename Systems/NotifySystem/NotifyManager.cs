using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerCellStudio
{
    public delegate void OnNotifyChange(bool isOn, int notifyNum, int notifyValue);

    public sealed partial class NotifyManager : SingletonBase<NotifyManager>, IModule
    {
        private class  NotifyNode
        {
            public int index;
            public bool isOn;
            public int notifyNumber;
            public int notifyValue;
            public int parent;
            public HashSet<int> children;
            public event OnNotifyChange onNotifyChange;

            public void Notify()
            {
                onNotifyChange?.Invoke(isOn, notifyNumber, notifyValue);
            }

            public void ClearNotify()
            {
                onNotifyChange = null;
            }
        }

        private NotifyNode[] _nodes;
        // public LinkEvent notifyTreeChanged = new LinkEvent();

        public void OnInit()
        {
            if (_nodes != null) return;
            var Number = Enum.GetValues(typeof(NotifyType));
            _nodes = new NotifyNode[Number.Length];
            for (int i = 0; i < Number.Length; i++)
            {
                var node = new NotifyNode
                {
                    index = i,
                    isOn = false,
                    notifyValue = 0,
                    notifyNumber = 0,
                    parent = -1,
                    children = new HashSet<int>(),
                };
                _nodes[i] = node;
            }
            BindNodes();
        }

        // public void OnGameReset()
        // {
        //     ClearAll();
        // }

        private partial void BindNodes();
        
        private partial void BindNodes()
        {
            #region Test
        
            // SetNodeParent(NotifyType.B, NotifyType.A);
            // SetNodeParent(NotifyType.C, NotifyType.B);
            // SetNodeParent(NotifyType.D, NotifyType.B);
            // SetNotify(NotifyType.D, true, 51);
            // SetNotify(NotifyType.C, false, 20);
            // SetNotify(NotifyType.E, true, 1);
        
            #endregion
        }

        private NotifyNode GetNode(NotifyType type)
        {
            return _nodes[(int) type];
        }

        public void GetNotifyInfo(NotifyType type, out bool isOn, out int notifyNumber, out int notifyValue)
        {
            var node = GetNode(type);
            isOn = node.isOn;
            notifyNumber = node.notifyNumber;
            notifyValue = node.notifyValue;
        }

        private void SetNodeParent(NotifyType child, NotifyType parent)
        {
            var childNode = GetNode(child);
            var parentNode = GetNode(parent);
            if( childNode.children.Contains(parentNode.index) || parentNode.parent == childNode.index)
            {
                ModuleLog<NotifyManager>.LogError($"Can not set [{child}] as child node to [{parent}], because [{child}] is [{parent}]'s parent node!");
                return;
            }
            childNode.parent = parentNode.index;
            parentNode.children.Add(childNode.index);
        }

        private void RemoveNodeParent(NotifyType child, NotifyType parent)
        {
            var childNode = GetNode(child);
            var parentNode = GetNode(parent);
            childNode.parent = -1;
            parentNode.children.Remove(childNode.index);
        }

        private void ClearAll()
        {
            foreach (var notifyNode in _nodes)
            {
                notifyNode.isOn = false;
                notifyNode.notifyNumber = 0;
                notifyNode.children.Clear();
                notifyNode.parent = -1;
                notifyNode.ClearNotify();
            }
        }

        private void CalNodeNotify(NotifyNode node, bool isOn, int notifyValue)
        {
            if (node.children.Count > 0)
            {
                var tempNotifyNumber = 0;
                var tempNotifyValue = 0;
                foreach (var nodeChild in node.children)
                {
                    var childNode = _nodes[nodeChild];
                    if (!childNode.isOn) continue;
                    tempNotifyNumber++;
                    tempNotifyValue += childNode.notifyValue;
                }
                node.notifyValue = tempNotifyValue;
                node.notifyNumber = tempNotifyNumber;
            }
            else
            {
                node.notifyValue = notifyValue;
                node.notifyNumber = isOn ? 1 : 0;
            }
            
            node.isOn = node.notifyNumber > 0;
            node.Notify();
            if (node.parent < 0 || node.parent >= _nodes.Length)
            {
                // notifyTreeChanged?.Invoke();
                return;
            }
            var parent = _nodes[node.parent];
            CalNodeNotify(parent, isOn, notifyValue);
        }

        public void ReCalNodeNotify(NotifyType nodeType)
        {
            var node = GetNode(nodeType);
            if (node.children.Count >= 0)
            {
                node.notifyNumber = node.children.Count(o => GetNode((NotifyType) o).isOn);
                node.notifyValue = node.children.Sum(o => GetNode((NotifyType) o).notifyValue);
            }
            node.isOn = node.notifyNumber > 0;
            CalNodeNotify(node, node.isOn, node.notifyValue);
        }

        public void SetNotify(NotifyType nodeType, bool isOn, int notifyValue = 0)
        {
            var node = GetNode(nodeType);
            if (node.children.Count > 0)
            {
                ModuleLog<NotifyManager>.LogError($"Can not set [{nodeType}], because [{nodeType}] is driven by its child nodes!");
                return;
            }  
            if (node.isOn == isOn && notifyValue == 0) return;
            CalNodeNotify(node, isOn, notifyValue);
        }
        
        public void ForceNotify(NotifyType nodeType, bool isOn, int notifyValue = 0)
        {
            var node = GetNode(nodeType);
            node.isOn = isOn;
            node.notifyValue = notifyValue;
            node.notifyNumber = isOn ? Mathf.Max(1, node.notifyNumber + 1) : 0;
            node.Notify();
        }

        public void Register(NotifyType nodeType, OnNotifyChange fun)
        {
            var node = GetNode(nodeType);
            node.onNotifyChange += fun;
        }

        public void UnRegister(NotifyType nodeType, OnNotifyChange fun)
        {
            var node = GetNode(nodeType);
            node.onNotifyChange -= fun;
        }

        public bool IsNotifyOn(NotifyType nodeType, out int notifyNum)
        {
            var node = GetNode(nodeType);
            notifyNum = node.notifyNumber;
            return node.isOn;
        }

        public IEnumerable<NotifyType> GetChildren(NotifyType notifyType, bool isOnOnly = false)
        {
            var node = GetNode(notifyType);
            if(node.children.Count == 0) yield break;
            foreach (var nodeChild in node.children)
            {
                if (isOnOnly && !_nodes[nodeChild].isOn) continue;
                yield return (NotifyType) nodeChild;
            }
        }
        
        public NotifyType GetParent(NotifyType notifyType)
        {
            var node = GetNode(notifyType);
            if (node.parent == -1) return NotifyType.Root;
            return (NotifyType) node.parent;            
        }
        
    }

}
