using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    public sealed class NotifyManager : SingletonBase<NotifyManager>, IModule
    {
        private Dictionary<Type, NotifyNode[]>  _nodes;

        private Dictionary<Type, object> _translators;
        // public LinkEvent notifyTreeChanged = new LinkEvent();

        /// <summary>
        /// 初始化通知节点树。
        /// </summary>
        public void OnInit()
        {
            if (_nodes != null) return;
            _nodes = new Dictionary<Type, NotifyNode[]>();
            _translators = new Dictionary<Type, object>();
            BindNodes();
        }

        /// <summary>
        /// Sets up a notification group based on the specified enum type.
        /// 设置一个通知分组，基于指定的枚举类型。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SetNotifyGroup<T>() where T : Enum
        {
            if (_nodes == null) OnInit();
            var enumType = typeof(T);
            if (_nodes.ContainsKey(enumType))
            {
                ModuleLogger.LogWarning<NotifyManager>($"Notify group for enum type {enumType} already exists.");
                return;
            }
            var values = (T[])Enum.GetValues(typeof(T));
            var translator = new EnumNotifyIndexTranslator<T>(values);
            var notifyNumber = translator.GetNotifyCount();
            var nodes = new NotifyNode[notifyNumber];
            for (int i = 0; i < notifyNumber; i++)
            {
                var node = new NotifyNode
                {
                    index = i,
                    isOn = false,
                    notifyValue = 0,
                    notifyNumber = 0,
                    parent = -1,
                    children = new HashSet<int>(),
                    otherGroupChildren = new List<OtherGroupNode>(),
                };
                nodes[i] = node;
            }
            _nodes[enumType] = nodes;
            _translators[enumType] = translator;
        }

        public void SetNotifyGroupByType(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum)
            {
                ModuleLogger.LogError<NotifyManager>($"Type {enumType} is not an enum type.");
                return;
            }
            // 反射调用SetNotifyGroup<T>();
            GetType()
                .GetMethod("SetNotifyGroup", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                ?.MakeGenericMethod(enumType)
                .Invoke(this, null);
        }
        
        /// <summary>
        /// Removes a notification group and clears all its nodes and relationships.
        /// 移除一个通知组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveNotifyGroup<T>() where T :Enum
        {
            var enumType = typeof(T);
            if (!_nodes.TryGetValue(enumType, out var nodes))
            {
                ModuleLogger.LogWarning<NotifyManager>($"Notify group for enum type {enumType} does not exist.");
                return;
            }
            foreach (var notifyNode in nodes)
            {
                notifyNode.ClearNotify();
                if (notifyNode.otherGroupParent != null)
                {
                    var parentGroup = _nodes[notifyNode.otherGroupParent.nodeType];
                    var parentNode = parentGroup[notifyNode.otherGroupParent.nodeIndex];
                    parentNode.otherGroupChildren.RemoveAll(n => n.nodeType == enumType && n.nodeIndex == notifyNode.index);
                    notifyNode.otherGroupParent = null;
                }
                for (var i = 0; i < notifyNode.otherGroupChildren.Count; i++)
                {
                    var otherNodeInfo = notifyNode.otherGroupChildren[i];
                    var group = _nodes[otherNodeInfo.nodeType];
                    var otherNode = group[otherNodeInfo.nodeIndex];
                    otherNode.otherGroupParent = null;
                }
                notifyNode.otherGroupChildren.Clear();
            }
            _crossGroupRelations.Remove(enumType);
            foreach (var crossGroupRelation in _crossGroupRelations)
            {
                crossGroupRelation.Value.Remove(enumType);
            }
            _nodes.Remove(enumType);
            _translators.Remove(enumType);
        }

        /// <summary>
        /// Gets the enum types that have been registered as notification groups.
        /// 获取已经注册为通知分组的枚举类型。
        /// </summary>
        /// <returns>Registered notification group types | 已注册的通知分组类型</returns>
        public IReadOnlyList<Type> GetNotifyGroupTypes()
        {
            if (_nodes == null || _nodes.Count == 0)
                return Array.Empty<Type>();
            return new List<Type>(_nodes.Keys);
        }

        /// <summary>
        /// Gets the number of nodes in a notification group.
        /// 获取通知分组中的节点数量。
        /// </summary>
        public int GetNotifyGroupCount(Type enumType)
        {
            return _nodes != null && enumType != null && _nodes.TryGetValue(enumType, out var nodes)
                ? nodes.Length
                : 0;
        }

        /// <summary>
        /// Gets node state and hierarchy information by enum type and translated index.
        /// 根据枚举类型和转换后的索引获取节点状态及层级信息。
        /// </summary>
        public bool TryGetNotifyNodeInfo(Type enumType, int notifyIndex, out bool isOn, out int notifyNumber,
            out int notifyValue, out int parentIndex, out IReadOnlyCollection<int> children)
        {
            isOn = false;
            notifyNumber = 0;
            notifyValue = 0;
            parentIndex = -1;
            children = Array.Empty<int>();

            if (_nodes == null || enumType == null || !_nodes.TryGetValue(enumType, out var nodes) ||
                (uint)notifyIndex >= (uint)nodes.Length)
                return false;

            var node = nodes[notifyIndex];
            isOn = node.isOn;
            notifyNumber = node.notifyNumber;
            notifyValue = node.notifyValue;
            parentIndex = node.parent;
            children = node.children;
            return true;
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

        private bool TryGetNode<T>(T type, out NotifyNode node, out Type enumType, out EnumNotifyIndexTranslator<T> translator) where T : Enum
        {
            enumType = typeof(T);
            _translators.TryGetValue(enumType, out var outValue);
            translator = outValue as EnumNotifyIndexTranslator<T>;
            if (translator == null)
            {
                ModuleLogger.LogError<NotifyManager>($"Translator for enum type {enumType} not found.");
                node = null;
                return false;
            }

            if (!_nodes.TryGetValue(enumType, out var nodes))
            {
                node = null;
                return false;
            }
            var index = translator.ToIndex(type);
            if (index < 0 || index >= nodes.Length)
            {
                ModuleLogger.LogError<NotifyManager>($"Index {index} for enum value {type} is out of range for enum type {enumType}.");
                node = null;
                return false;
            }
            node = nodes[index];
            return true;
        }

        /// <summary>
        /// Gets the notification information of the specified node.
        /// 获取指定节点的通知信息。
        /// </summary>
        /// <param name="type">Notification type | 通知类型</param>
        /// <param name="isOn">Whether the notification is active | 是否激活</param>
        /// <param name="notifyNumber">Number of active notifications | 通知数量</param>
        /// <param name="notifyValue">Notification value | 通知值</param>
        public void GetNotifyInfo<T>(T type, out bool isOn, out int notifyNumber, out int notifyValue) where T : Enum
        {
            if (TryGetNode(type, out var node, out _, out _))
            {
                isOn = node.isOn;
                notifyNumber = node.notifyNumber;
                notifyValue = node.notifyValue;
                return;
            }
            isOn = false;
            notifyNumber = 0;
            notifyValue = 0;
        }

        internal void GetNotifyInfo(Type enumType, int notifyIndex, out bool isOn, out int notifyNumber,
            out int notifyValue)
        {
            if (_nodes.TryGetValue(enumType, out var nodes) && notifyIndex >= 0 && notifyIndex < nodes.Length)
            {
                var node = nodes[notifyIndex];
                isOn = node.isOn;
                notifyNumber = node.notifyNumber;
                notifyValue = node.notifyValue;
                return;
            }
            isOn = false;
            notifyNumber = 0;
            notifyValue = 0;
        }

        private bool CheckIsChainLoop<T>(NotifyNode child, NotifyNode parent, NotifyNode[] nodes) where T : Enum
        {
#if UNITY_EDITOR
            if( child.children.Contains(parent.index) || parent.parent == child.index)
            {
                return true;
            }
            var checkNode = parent;
            while (checkNode.parent >= 0 && checkNode.parent < nodes.Length)
            {
                if (checkNode.parent == child.index)
                    return true;
                checkNode = nodes[checkNode.parent];
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
        public void SetNodeParent<T>(T child, T parent) where T : Enum
        {
            if (child.Equals(parent))
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{child}] as child node to himself");
                return;
            }
            var enumType = typeof(T);
            _translators.TryGetValue(enumType, out var outValue);
            var translator = outValue as EnumNotifyIndexTranslator<T>;
            if (translator == null)
            {
                ModuleLogger.LogError<NotifyManager>($"Translator for enum type {enumType} not found.");
                return;
            }
            if (!_nodes.TryGetValue(enumType, out var nodes))
            {
                ModuleLogger.LogError<NotifyManager>($"Nodes for enum type {enumType} not found.");
                return;
            }
            var childIndex = translator.ToIndex(child);
            if (childIndex < 0 || childIndex >= nodes.Length)
            {
                ModuleLogger.LogError<NotifyManager>($"Index {childIndex} for enum value {child} is out of range for enum type {enumType}.");
                return;
            }
            var parentIndex = translator.ToIndex(parent);
            if (parentIndex < 0 || parentIndex >= nodes.Length)
            {
                ModuleLogger.LogError<NotifyManager>($"Index {parentIndex} for enum value {parent} is out of range for enum type {enumType}.");
                return;
            }
            var childNode = nodes[childIndex];
            var parentNode = nodes[parentIndex];
            if (CheckIsChainLoop<T>(childNode, parentNode, nodes))
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{child}] as child node to [{parent}], because the two nodes forming a loop");
                return;
            }
            if (childNode.parent >= 0)
            {
                var oldParentNode = nodes[childNode.parent];
                oldParentNode.children.Remove(childNode.index);
                ReCalNodeNotifyInternal(oldParentNode, nodes);
            }
            childNode.parent = parentNode.index;
            parentNode.children.Add(childNode.index);
            ReCalNodeNotifyInternal(parentNode, nodes);
        }

        /// <summary>
        /// Removes the parent-child relationship between notification nodes.
        /// 移除通知节点的父子关系。
        /// </summary>
        /// <param name="child"></param>
        /// <typeparam name="T"></typeparam>
        public void RemoveNodeParent<T>(T child) where T : Enum
        {
            var enumType = typeof(T);
            _translators.TryGetValue(enumType, out var outValue);
            var translator = outValue as EnumNotifyIndexTranslator<T>;
            if (translator == null)
            {
                ModuleLogger.LogError<NotifyManager>($"Translator for enum type {enumType} not found.");
                return;
            }

            if (!_nodes.TryGetValue(enumType, out var nodes))
            {
                ModuleLogger.LogError<NotifyManager>($"Nodes for enum type {enumType} not found.");
                return;
            }
            var childIndex = translator.ToIndex(child);
            if (childIndex < 0 || childIndex >= nodes.Length)
            {
                ModuleLogger.LogError<NotifyManager>($"Index {childIndex} for enum value {child} is out of range for enum type {enumType}.");
                return;
            }
            
            var childNode = nodes[childIndex];
            var parentIndex = childNode.parent;
            if (parentIndex == -1) return;
            
            if (parentIndex < 0 || parentIndex >= nodes.Length)
            {
                ModuleLogger.LogError<NotifyManager>($"Index {parentIndex} is out of range for enum type {enumType}.");
                return;
            }

            var parentNode = nodes[parentIndex];
            childNode.parent = -1;
            parentNode.children.Remove(childNode.index);
            ReCalNodeNotifyInternal(parentNode, nodes);
        }
        
        private Dictionary<Type, HashSet<Type>> _crossGroupRelations = new Dictionary<Type, HashSet<Type>>();

        private bool CheckIsGroupLoop(Type parentType, Type childType)
        {
            var stack = ListPool<Type>.Get();
            var visited = HashSetPool<Type>.Get();
            stack.Add(childType);
            visited.Add(childType);
            var point = 0;
            var result = false;
            while (point < stack.Count)
            {
                var currentType = stack[point];

                if (_crossGroupRelations.TryGetValue(currentType, out var relatedTypes))
                {
                    if (relatedTypes.Contains(parentType))
                    {
                        result = true;
                        break;
                    }
                    foreach (var relatedType in relatedTypes)
                    {
                        if (visited.Contains(relatedType)) continue;
                        stack.Add(relatedType);
                        visited.Add(relatedType);
                    }
                }
                point++;
            }
            ListPool<Type>.Release(stack);
            HashSetPool<Type>.Release(visited);
            return result;
        }
        
        /// <summary>
        /// Sets the parent-child relationship between notification nodes across groups.
        /// 设置节点的父节点
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        public void SetNodeParentCrossGroup<T, K>(T child, K parent)
            where T : Enum
            where K : Enum
        {
            if (!TryGetNode(child, out var childNode, out var childType, out _))
                return;
            if (!TryGetNode(parent, out var parentNode, out var parentType, out _))
                return;
            
            if (childNode.otherGroupParent != null && childNode.otherGroupParent.nodeType == parentType)
                return;
            if (childType == parentType)
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{child}] as child node to [{parent}], because the two nodes are in the same group");
                return;
            }
            if (parentNode.otherGroupChildren.Any(n => n.nodeType == childType && n.nodeIndex == childNode.index))
                return;
            
            // // parent必须是叶节点
            // if (parentNode.children.Count > 0)
            // {
            //     ModuleLogger.LogError<NotifyManager>($"Can not set [{parent}] as parent node to [{child}], because [{parent}] is not a leaf node");
            //     return;
            // }
            if (CheckIsGroupLoop(parentType, childType))
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{child}] as child node to [{parent}], because the two nodes forming a loop across groups");
                return;
            }
            RemoveNodeParentCrossGroup(child);
            
            childNode.otherGroupParent = new OtherGroupNode()
            {
                nodeType = parentType,
                nodeIndex = parentNode.index
            };
            
            parentNode.otherGroupChildren.Add(new OtherGroupNode()
            {
                nodeType = childType,
                nodeIndex = childNode.index
            });
            
            if (_crossGroupRelations.TryGetValue(parentType, out var relatedTypes))
            {
                relatedTypes.Add(childType);
            }
            else
            {
                _crossGroupRelations[parentType] = new HashSet<Type> { childType };
            }
            ReCalNodeNotifyInternal(parentNode, _nodes[parentType]);
        }
        
        /// <summary>
        /// Removes the parent-child relationship between notification nodes across groups.
        /// 移除节点的父节点
        /// </summary>
        /// <param name="child"></param>
        /// <typeparam name="T"></typeparam>
        public void RemoveNodeParentCrossGroup<T>(T child)
            where T : Enum
        {
            if (!TryGetNode(child, out var childNode, out var childType, out _))
                return;
            var parentNodeInfo = childNode.otherGroupParent;
            if (parentNodeInfo == null) return;
            
            childNode.otherGroupParent = null;

            Type parentType = parentNodeInfo.nodeType;
            if (parentType == null)
            {
                ModuleLogger.LogError<NotifyManager>($"Parent type for child [{child}] not found in cross-group relations.");
                return;
            }
            if (_crossGroupRelations.TryGetValue(parentType, out var relatedTypes))
            {
                relatedTypes.Remove(childType);
                if (relatedTypes.Count == 0)
                {
                    _crossGroupRelations.Remove(parentType);
                }
            }
            if (!_nodes.TryGetValue(parentType, out var parentNodes))
            {
                ModuleLogger.LogError<NotifyManager>($"Parent nodes for enum type {parentType} not found.");
                return;
            }
            if (parentNodeInfo.nodeIndex < 0 || parentNodeInfo.nodeIndex >= parentNodes.Length)
            {
                ModuleLogger.LogError<NotifyManager>($"Parent node index {parentNodeInfo.nodeIndex} is out of range for enum type {parentType}.");
                return;
            }
            var parentNode = parentNodes[parentNodeInfo.nodeIndex];
            parentNode.otherGroupChildren.RemoveAll(n => n.nodeType == childType && n.nodeIndex == childNode.index);
            ReCalNodeNotifyInternal(parentNode, parentNodes);
        }

        private void CalNodeNotify(NotifyNode node, bool isOn, int notifyValue, NotifyNode[] nodeGroup)
        {
            var tempNotifyNumber = 0;
            var tempNotifyValue = 0;
            if (node.children.Count > 0)
            {
                foreach (var nodeChild in node.children)
                {
                    var childNode = nodeGroup[nodeChild];
                    if (!childNode.isOn) continue;
                    tempNotifyNumber++;
                    tempNotifyValue += childNode.notifyValue;
                }
            }
            
            if (node.otherGroupChildren.Count > 0)
            {
                foreach (var otherNodeInfo in node.otherGroupChildren)
                {
                    if (!_nodes.TryGetValue(otherNodeInfo.nodeType, out var otherGroup)) continue;
                    if (otherNodeInfo.nodeIndex < 0 || otherNodeInfo.nodeIndex >= otherGroup.Length) continue;
                    var otherNode = otherGroup[otherNodeInfo.nodeIndex];
                    if (!otherNode.isOn) continue;
                    tempNotifyNumber++;
                    tempNotifyValue += otherNode.notifyValue;
                }
            }
            else if (isOn)
            {
                tempNotifyNumber++;
                tempNotifyValue += notifyValue;
            }
            
            var nodeIsOn = tempNotifyNumber > 0;
            if (tempNotifyValue == node.notifyValue && tempNotifyNumber == node.notifyNumber && nodeIsOn == node.isOn) return;
            
            node.notifyValue = tempNotifyValue;
            node.notifyNumber = tempNotifyNumber;
            node.isOn = nodeIsOn;
            node.Notify();
            
            if (node.otherGroupParent != null)
            {
                var parent = node.otherGroupParent;
                if (_nodes.TryGetValue(parent.nodeType, out var parentGroup) 
                    && parent.nodeIndex >= 0
                    && parent.nodeIndex < parentGroup.Length)
                {
                    var parentNode = parentGroup[parent.nodeIndex];
                    CalNodeNotify(parentNode, nodeIsOn, tempNotifyValue, parentGroup);
                }
            }
            
            if (node.parent >= 0 && node.parent < nodeGroup.Length)
            {
                var parent = nodeGroup[node.parent];
                CalNodeNotify(parent, nodeIsOn, tempNotifyValue, nodeGroup);
            }
        }
        
        private void ReCalNodeNotifyInternal(NotifyNode node, NotifyNode[] nodeGroup)
        {
            if (node == null) return;
            CalNodeNotify(node, node.isOn, node.notifyValue, nodeGroup);
        }

        /// <summary>
        /// Recalculates the notification state of the specified node.
        /// 重新计算指定节点的通知状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        public void ReCalNodeNotify<T>(T nodeType) where T : Enum
        {
            if (!TryGetNode(nodeType, out var node, out var enumType, out _)) return;
            var nodeGroup = _nodes[enumType];
            ReCalNodeNotifyInternal(node, nodeGroup);
        }

        /// <summary>
        /// Clears the node state and recalculates through the tree structure upward.
        /// 清空节点状态，并通过树结构向上计算节点状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        public void ClearNodeNotify<T>(T nodeType) where T : Enum
        {
            if (!TryGetNode(nodeType, out var node, out var enumType, out _)) return;
            var nodes = _nodes[enumType];
            ClearNodeNotify(node, nodes);
            ReCalNodeNotifyInternal(node, nodes);
        }

        private void ClearNodeNotify(NotifyNode node, NotifyNode[] nodes)
        {
            node.notifyValue = 0;
            node.notifyNumber = 0;
            node.isOn = false;
            node.Notify();
            foreach (var nodeChild in node.children)
            {
                var childNode = nodes[nodeChild];
                if (!childNode.isOn) continue;
                ClearNodeNotify(childNode, nodes);
            }
            foreach (var nodeOtherGroupChild in node.otherGroupChildren)
            {
                if (_nodes.TryGetValue(nodeOtherGroupChild.nodeType, out var otherGroup) 
                    && nodeOtherGroupChild.nodeIndex >= 0
                    && nodeOtherGroupChild.nodeIndex < otherGroup.Length)
                {
                    var childNode = otherGroup[nodeOtherGroupChild.nodeIndex];
                    if (!childNode.isOn) continue;
                    ClearNodeNotify(childNode, otherGroup);
                }
            }
        }
        
        /// <summary>
        /// Clears all notification states and relationships.
        /// 清除所有通知状态和关系。
        /// </summary>
        public void ClearAll()
        {
            foreach (var nodes in _nodes.Values)
            {
                for (var i = 0; i < nodes.Length; i++)
                {
                    var notifyNode = nodes[i];
                    notifyNode.isOn = false;
                    notifyNode.notifyNumber = 0;
                    notifyNode.notifyValue = 0;
                    notifyNode.parent = -1;
                    notifyNode.children.Clear();
                    notifyNode.otherGroupParent = null;
                    notifyNode.otherGroupChildren.Clear();
                    notifyNode.ClearNotify();
                }
            }
            _crossGroupRelations.Clear();
        }

        /// <summary>
        /// Sets the notification state of a node.
        /// 设置通知节点的状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="isOn">Whether to activate the notification | 是否激活通知</param>
        /// <param name="notifyValue">Notification value | 通知值</param>
        public void SetNotify<T>(T nodeType, bool isOn, int notifyValue = 0) where T : Enum
        {
            if (!TryGetNode(nodeType, out var node, out var enumType, out _)) return;
            if (node.children.Count > 0 || node.otherGroupChildren.Count > 0)
            {
                ModuleLogger.LogError<NotifyManager>($"Can not set [{nodeType}], because [{nodeType}] is driven by its child nodes!");
                return;
            }  
            if (node.isOn == isOn && node.notifyValue == notifyValue) return;
            var nodeGroup = _nodes[enumType];
            CalNodeNotify(node, isOn, notifyValue, nodeGroup);
        }
        
        /// <summary>
        /// Forces a node's notification state to change.
        /// 强制更改节点的通知状态。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="isOn">Whether to activate the notification | 是否激活通知</param>
        /// <param name="notifyValue">Notification value | 通知值</param>
        public void ForceNotify<T>(T nodeType, bool isOn, int notifyValue = 0) where T : Enum
        {
            if (!TryGetNode(nodeType, out var node, out var enumType, out _)) return;
            node.isOn = isOn;
            node.notifyValue = notifyValue;
            var nodes = _nodes[enumType];
            if (isOn)
            {
                var onNumber = 0;
                foreach (var nodeChild in node.children)
                {
                    var childNode = nodes[nodeChild];
                    if (childNode.isOn)
                        onNumber++;
                }
                foreach (var nodeOtherGroupChild in node.otherGroupChildren)
                {
                    if (_nodes.TryGetValue(nodeOtherGroupChild.nodeType, out var parentGroup)
                        && nodeOtherGroupChild.nodeIndex >= 0
                        && nodeOtherGroupChild.nodeIndex < parentGroup.Length)
                    {
                        var otherGroupParent = parentGroup[nodeOtherGroupChild.nodeIndex];
                        if (otherGroupParent.isOn)
                            onNumber++;
                    }
                }
                node.notifyNumber = Mathf.Max(1, onNumber);
            }
            else
            {
                node.notifyNumber = 0;
            }
            node.Notify();
            if (node.parent >= 0 && node.parent < nodes.Length)
            {
                var parentNode = nodes[node.parent];
                ReCalNodeNotifyInternal(parentNode, nodes);
            }
            if (node.otherGroupParent != null)
            {
                var parent = node.otherGroupParent;
                if (_nodes.TryGetValue(parent.nodeType, out var parentGroup) 
                    && parent.nodeIndex >= 0
                    && parent.nodeIndex < parentGroup.Length)
                {
                    var parentNode = parentGroup[parent.nodeIndex];
                    ReCalNodeNotifyInternal(parentNode, parentGroup);
                }
            }
        }

        /// <summary>
        /// Registers a notification callback.
        /// 注册通知回调。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="fun">Callback function | 回调函数</param>
        public void Register<T>(T nodeType, OnNotifyChange fun) where T : Enum
        {
            SetNotifyGroup<T>();
            if (!TryGetNode(nodeType, out var node, out _, out _)) return;
            node.onNotifyChange += fun;
        }
        
        internal void Register(Type enumType, int nodeTypeIndex, OnNotifyChange fun)
        {
            if (enumType == null) return;
            SetNotifyGroupByType(enumType);
            if (!_nodes.TryGetValue(enumType, out var nodes) || nodeTypeIndex < 0 ||
                nodeTypeIndex >= nodes.Length) return;
            var node = nodes[nodeTypeIndex];
            node.onNotifyChange += fun;
        }

        /// <summary>
        /// Unregisters a notification callback.
        /// 注销通知回调。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="fun">Callback function | 回调函数</param>
        public void UnRegister<T>(T nodeType, OnNotifyChange fun) where T : Enum
        {
            if (!TryGetNode(nodeType, out var node, out _, out _)) return;
            node.onNotifyChange -= fun;
        }

        internal void UnRegister(Type enumType, int nodeTypeIndex, OnNotifyChange fun)
        {
            if (enumType == null || !enumType.IsEnum) return;
            if (!_nodes.TryGetValue(enumType, out var nodes)) return;
            if (nodeTypeIndex < 0 || nodeTypeIndex >= nodes.Length) return;
            var node = nodes[nodeTypeIndex];
            node.onNotifyChange -= fun;
        }

        /// <summary>
        /// Checks if a notification node is active.
        /// 检查通知节点是否激活。
        /// </summary>
        /// <param name="nodeType">Node type | 节点类型</param>
        /// <param name="notifyNum">Number of active notifications | 通知数量</param>
        /// <returns>Whether the notification is active | 是否激活</returns>
        public bool IsNotifyOn<T>(T nodeType, out int notifyNum) where T : Enum
        {
            notifyNum = 0;
            if (!TryGetNode(nodeType, out var node, out _, out _)) return false;
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
        public IEnumerable<T> GetChildren<T>(T notifyType, bool isOnOnly = false) where T : Enum
        {
            if (!TryGetNode(notifyType, out var node, out var enumType, out var translator)) yield break;
            if(node.children.Count == 0) yield break;
            var nodes = _nodes[enumType];
            foreach (var nodeChild in node.children)
            {
                if (isOnOnly && !nodes[nodeChild].isOn) continue;
                yield return translator.ByIndex(nodeChild);
            }
        }

        /// <summary>
        /// Gets the parent node of the specified node.
        /// 获取指定节点的父节点。
        /// </summary>
        /// <param name="notifyType">Node type | 节点类型</param>
        /// <returns>Parent node type | 父节点类型</returns>
        public T GetParent<T>(T notifyType) where T : Enum
        {
            if (!TryGetNode(notifyType, out var node, out _, out var translator)) return default;
            if (node.parent == -1) return default;
            return translator.ByIndex(node.parent);            
        }

        /// <summary>
        /// Add `Notifier` Component on target UI node.
        /// 在UI组件上添加 Notifier 组件。
        /// </summary>
        /// <param name="uiNode">UI Node | UI节点</param>
        /// <param name="notifyType">Notify Node type | 节点类型</param>
        public static void AddNotifer<T>(RectTransform uiNode, T notifyType)  where T : Enum
        {
            if (uiNode == null) return;
            var notifer = uiNode.GetComponentInChildren<Notifier>(true);
            if (notifer)
            {
                var enumType = typeof(T);
                if (notifer.notifyType == enumType)
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
                        notifier.Init(notifyType);
                    }
                }, PoolManager.PoolGroupName.UI);
            }
        }        
    }

}
