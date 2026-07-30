using System;
using System.Collections.Generic;
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

        /// <summary>
        /// 初始化通知节点树。
        /// </summary>
        public void OnInit()
        {
            if (_nodes != null) return;
            var notifyNumber = Enum.GetValues(typeof(NotifyType));
            _nodes = new NotifyNode[notifyNumber.Length];
            for (int i = 0; i < notifyNumber.Length; i++)
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

        private void BindNodes()
        {
            var allPresets =  ReflectionUtils.GetInstantiableSubtypeInstance<INotifyBindPreset>();
            if (allPresets == null || allPresets.Count == 0) return;
            for (var i = 0; i < allPresets.Count; i++)
            {
                var preset = allPresets[i];
                preset.BindNodes(this);
            }
        }

        private NotifyNode GetNode(NotifyType type)
        {
            return _nodes[(int) type];
        }

        /// <summary>
        /// Gets the notification information of the specified node.
        /// 获取指定节点的通知信息。
        /// </summary>
        /// <param name="type">Notification type | 通知类型</param>
        /// <param name="isOn">Whether the notification is active | 是否激活</param>
        /// <param name="notifyNumber">Number of active notifications | 通知数量</param>
        /// <param name="notifyValue">Notification value | 通知值</param>
        public void GetNotifyInfo(NotifyType type, out bool isOn, out int notifyNumber, out int notifyValue)
        {
            var node = GetNode(type);
            isOn = node.isOn;
            notifyNumber = node.notifyNumber;
            notifyValue = node.notifyValue;
        }

        private bool CheckIsChainLoop(NotifyNode child, NotifyNode parent)
        {
#if UNITY_EDITOR
            if( child.children.Contains(parent.index) || parent.parent == child.index)
            {
                return true;
            }
            var checkNode = parent;
            while (checkNode.parent >= 0)
            {
                if (checkNode.parent == child.index)
                    return true;
                checkNode = GetNode((NotifyType)checkNode.parent);
            }
#endif
            return false;
        }

        /// <summary>
        /// Sets the parent-child relationship between notification nodes.
        /// 设置通知节点的父子关系。
        /// </summary>
        /// <param name="child">Child node type | 子节点类型</param>
        /// <param name="parent">Parent node type | 父节点类型</param>
        public void SetNodeParent(NotifyType child, NotifyType parent)
        {
            if (child == parent)
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{child}] as child node to himself");
                return;
            }
            var childNode = GetNode(child);
            var parentNode = GetNode(parent);
            if (CheckIsChainLoop(childNode, parentNode))
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{child}] as child node to [{parent}], because the two nodes forming a loop");
                return;
            }
            if (childNode.parent >= 0)
            {
                var oldParentNode = _nodes[childNode.parent];
                oldParentNode.children.Remove(childNode.index);
            }
            childNode.parent = parentNode.index;
            parentNode.children.Add(childNode.index);
        }

        public void RemoveNodeParent(NotifyType child, NotifyType parent)
        {
            var childNode = GetNode(child);
            var parentNode = GetNode(parent);
            childNode.parent = -1;
            parentNode.children.Remove(childNode.index);
        }

        /// <summary>
        /// Clears all notification states and relationships.
        /// 清除所有通知状态和关系。
        /// </summary>
        public void ClearAll()
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

        /// <summary>
        /// Recalculates the notification state of the specified node.
        /// 重新计算指定节点的通知状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        public void ReCalNodeNotify(NotifyType nodeType)
        {
            var node = GetNode(nodeType);
            CalNodeNotify(node, node.isOn, node.notifyValue);
        }

        /// <summary>
        /// Clears the node state and recalculates through the tree structure upward.
        /// 清空节点状态，并通过树结构向上计算节点状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        public void ClearNodeNotify(NotifyType nodeType)
        {
            var node = GetNode(nodeType);
            ClearNodeNotify(node);
            ReCalNodeNotify(nodeType);
        }

        private void ClearNodeNotify(NotifyNode node)
        {
            node.notifyValue = 0;
            node.notifyNumber = 0;
            node.isOn = false;
            node.Notify();
            foreach (var nodeChild in node.children)
            {
                var childNode = _nodes[nodeChild];
                if (!childNode.isOn) continue;
                ClearNodeNotify(childNode);
            }
        }

        /// <summary>
        /// Sets the notification state of a node.
        /// 设置通知节点的状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="isOn">Whether to activate the notification | 是否激活通知</param>
        /// <param name="notifyValue">Notification value | 通知值</param>
        public void SetNotify(NotifyType nodeType, bool isOn, int notifyValue = 0)
        {
            var node = GetNode(nodeType);
            if (node.children.Count > 0)
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{nodeType}], because [{nodeType}] is driven by its child nodes!");
                return;
            }  
            if (node.isOn == isOn && node.notifyValue == notifyValue) return;
            CalNodeNotify(node, isOn, notifyValue);
        }
        
        /// <summary>
        /// Forces a node's notification state to change.
        /// 强制更改节点的通知状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="isOn">Whether to activate the notification | 是否激活通知</param>
        /// <param name="notifyValue">Notification value | 通知值</param>
        public void ForceNotify(NotifyType nodeType, bool isOn, int notifyValue = 0)
        {
            var node = GetNode(nodeType);
            node.isOn = isOn;
            node.notifyValue = notifyValue;
            if (node.children.Count > 0 && isOn)
            {
                var onNumber = 0;
                foreach (var nodeChild in node.children)
                {
                    var childNode = _nodes[nodeChild];
                    if (childNode.isOn)
                        onNumber++;
                }
                node.notifyNumber = Mathf.Max(1, onNumber);
            }
            else
            {
                node.notifyNumber = isOn ? 1 : 0;
            }
            node.Notify();
            if (node.parent >= 0)
                ReCalNodeNotify((NotifyType)node.parent);
        }

        /// <summary>
        /// Registers a notification callback.
        /// 注册通知回调。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="fun">Callback function | 回调函数</param>
        public void Register(NotifyType nodeType, OnNotifyChange fun)
        {
            var node = GetNode(nodeType);
            node.onNotifyChange += fun;
        }

        /// <summary>
        /// Unregisters a notification callback.
        /// 注销通知回调。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="fun">Callback function | 回调函数</param>
        public void UnRegister(NotifyType nodeType, OnNotifyChange fun)
        {
            var node = GetNode(nodeType);
            node.onNotifyChange -= fun;
        }

        /// <summary>
        /// Checks if a notification node is active.
        /// 检查通知节点是否激活。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="notifyNum">Number of active notifications | 通知数量</param>
        /// <returns>Whether the notification is active | 是否激活</returns>
        public bool IsNotifyOn(NotifyType nodeType, out int notifyNum)
        {
            var node = GetNode(nodeType);
            notifyNum = node.notifyNumber;
            return node.isOn;
        }

        /// <summary>
        /// Gets all child nodes of the specified node.
        /// 获取指定节点的所有子节点。
        /// </summary>
        /// <param name="notifyType">Node type | 节点类型</param>
        /// <param name="isOnOnly">Only return active child nodes | 是否只返回激活的子节点</param>
        /// <returns>Collection of child nodes | 子节点集合</returns>
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

        /// <summary>
        /// Gets the parent node of the specified node.
        /// 获取指定节点的父节点。
        /// </summary>
        /// <param name="notifyType">Node type | 节点类型</param>
        /// <returns>Parent node type | 父节点类型</returns>
        public NotifyType GetParent(NotifyType notifyType)
        {
            var node = GetNode(notifyType);
            if (node.parent == -1) return NotifyType.Root;
            return (NotifyType) node.parent;            
        }

        /// <summary>
        /// Add `Notifier` Component on target UI node.
        /// 在UI组件上添加 Notifier 组件。
        /// </summary>
        /// <param name="uiNode">UI Node | UI节点</param>
        /// <param name="notifyType">Notify Node type | 节点类型</param>
        public static void AddNotifer(RectTransform uiNode, NotifyType notifyType)
        {
            if (uiNode == null) return;
            var notifer = uiNode.GetComponentInChildren<Notifier>(true);
            if (notifer)
            {
                if (notifer.notifyType == notifyType)
                    return;

                notifer.Init(notifyType);
            }
            else
            {
                PoolManager.instance.GetGameObjectAsync("Assets/Res/UI/Common/RedPoint.prefab", o =>
                {
                    var notifier = o.GetComponent<Notifier>();
                    if (notifier)
                    {
                        notifier.notifyType = notifyType;
                    }
                }, PoolManager.PoolGroupName.UI);
            }
        }        
    }

}
