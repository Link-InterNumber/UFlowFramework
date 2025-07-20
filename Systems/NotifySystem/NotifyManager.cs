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
                checkNode = GetNode((NotifyType)parent.parent);
                if (checkNode.index == child.index)
                    return true;
            }
#endif
            return false;
        }

        private void SetNodeParent(NotifyType child, NotifyType parent)
        {
            if (child == parent)
            {
                ModuleLog.LogError<NotifyManager>($"Can not set [{child}] as child node to himself");
                return;
            }
            var childNode = GetNode(child);
            var parentNode = GetNode(parent);
            if (CheckIsChainLoop(childNode, parentNode))
            {
                ModuleLog.LogError<NotifyManager>($"Can not set [{child}] as child node to [{parent}], because the two nodes forming a loop");
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

        /// <summary>
        /// 重新计算指定节点的通知状态。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        public void ReCalNodeNotify(NotifyType nodeType)
        {
            var node = GetNode(nodeType);
            CalNodeNotify(node, node.isOn, node.notifyValue);
        }

        /// <summary>
        /// 清空节点状态，并通过树结构向上计算节点状态
        /// </summary>
        /// <param name="nodeType">节点类型</param>
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
        /// 设置通知节点的状态。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="isOn">是否开启</param>
        /// <param name="notifyValue">通知值</param>
        public void SetNotify(NotifyType nodeType, bool isOn, int notifyValue = 0)
        {
            var node = GetNode(nodeType);
            if (node.children.Count > 0)
            {
                ModuleLog.LogError<NotifyManager>($"Can not set [{nodeType}], because [{nodeType}] is driven by its child nodes!");
                return;
            }  
            if (node.isOn == isOn && node.notifyValue == notifyValue) return;
            CalNodeNotify(node, isOn, notifyValue);
        }
        
        /// <summary>
        /// 强制通知节点状态变更。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="isOn">是否开启</param>
        /// <param name="notifyValue">通知值</param>
        public void ForceNotify(NotifyType nodeType, bool isOn, int notifyValue = 0)
        {
            var node = GetNode(nodeType);
            node.isOn = isOn;
            node.notifyValue = notifyValue;
            node.notifyNumber = isOn ? Mathf.Max(1, node.notifyNumber + 1) : 0;
            node.Notify();
            ReCalNodeNotify(nodeType);
        }

        /// <summary>
        /// 注册通知回调。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="fun">回调方法</param>
        public void Register(NotifyType nodeType, OnNotifyChange fun)
        {
            var node = GetNode(nodeType);
            node.onNotifyChange += fun;
        }

        /// <summary>
        /// 注销通知回调。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="fun">回调方法</param>
        public void UnRegister(NotifyType nodeType, OnNotifyChange fun)
        {
            var node = GetNode(nodeType);
            node.onNotifyChange -= fun;
        }

        /// <summary>
        /// 判断通知节点是否开启。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="notifyNum">通知数量</param>
        /// <returns>是否开启</returns>
        public bool IsNotifyOn(NotifyType nodeType, out int notifyNum)
        {
            var node = GetNode(nodeType);
            notifyNum = node.notifyNumber;
            return node.isOn;
        }

        /// <summary>
        /// 获取指定节点的所有子节点。
        /// </summary>
        /// <param name="notifyType">节点类型</param>
        /// <param name="isOnOnly">只返回开启的子节点</param>
        /// <returns>子节点集合</returns>
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
        /// 获取指定节点的父节点。
        /// </summary>
        /// <param name="notifyType">节点类型</param>
        /// <returns>父节点类型</returns>
        public NotifyType GetParent(NotifyType notifyType)
        {
            var node = GetNode(notifyType);
            if (node.parent == -1) return NotifyType.Root;
            return (NotifyType) node.parent;            
        }

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
