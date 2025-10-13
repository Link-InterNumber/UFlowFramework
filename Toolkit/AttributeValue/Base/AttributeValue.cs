using System;
using UnityEngine;

namespace PowerCellStudio
{
    public delegate void AttributeValueEvent<T>(T currentValue, T originValue, T prevValue);

    [Serializable]
    public class AttributeValue<T> : IAttributeValue<T>
    {
        public AttributeValue(T initValue)
        {
            Reset(initValue);
        }

        [SerializeField] protected T originValue;
        [SerializeField] protected T currentValue;
        protected T _prevValue;
        [SerializeField] protected AttributeActionContainer<T> actions;
        public event AttributeValueEvent<T> onValueChange;

        /// <summary>
        /// Gets the actions.
        /// 获取动作容器。
        /// </summary>
        public AttributeActionContainer<T> GetActions() { return actions; }

        private bool _isDirty = true;

        /// <summary>
        /// Checks if this instance is equal to another attribute value.
        /// 检查此实例是否与另一个属性值相等。
        /// </summary>
        /// <param name="other">The other attribute value to compare.
        /// 要比较的另一个属性值。</param>
        public bool Equals(IAttributeValue<T> other)
        {
            if (other == null) return false;
            return originValue.Equals(other.GetOrigin())
                && GetCurrent().Equals(other.GetCurrent())
                && actions.Count == other.GetActions().Count;
        }

        /// <summary>
        /// Checks if the values are equal to another attribute value.
        /// 检查值是否与另一个属性值相等。
        /// </summary>
        /// <param name="other">The other attribute value to compare.
        /// 要比较的另一个属性值。</param>
        public bool ValueEquals(IAttributeValue<T> other)
        {
            if (other == null) return false;
            return originValue.Equals(other.GetOrigin())
                && GetCurrent().Equals(other.GetCurrent());
        }

        /// <summary>
        /// Creates a deep clone of the current attribute value.
        /// 创建当前属性值的深度克隆。
        /// </summary>
        /// <returns>Clone of the attribute value.
        /// 属性值的克隆。</returns>
        public IAttributeValue<T> Clone()
        {
            var cloned = new AttributeValue<T>(originValue);
            cloned.actions = actions.Clone();
            return cloned;
        }

        /// <summary>
        /// Resets the action container.
        /// 重置动作容器。
        /// </summary>
        public void ResetAction()
        {
            actions.Clear();
            _prevValue = currentValue;
            currentValue = originValue;
            _isDirty = true;
        }

        /// <summary>
        /// Resets the attribute value to the specified initial value.
        /// 重置属性值为指定的初始值。
        /// </summary>
        /// <param name="initValue">The initial value to reset to.
        /// 要重置为的初始值。</param>
        public void Reset(T initValue)
        {
            originValue = initValue;
            _prevValue = initValue;
            currentValue = initValue;
            actions = new AttributeActionContainer<T>();
            _isDirty = true;
        }

        /// <summary>
        /// Sets the attribute value to a new value.
        /// 设置属性值到新值。
        /// </summary>
        /// <param name="newValue">The new value to set.
        /// 要设置的新值。</param>
        public void Set(T newValue)
        {
            originValue = newValue;
            Calculate();
        }

        /// <summary>
        /// Calculates the final value based on current and origin values.
        /// 根据当前值和原始值计算最终值。
        /// </summary>
        /// <returns>The calculated current value.
        /// 计算后的当前值。</returns>
        public T Calculate()
        {
            currentValue = originValue;
            foreach (var attributeAction in actions)
            {
                currentValue = attributeAction.Action(currentValue, originValue);
            }
            if (!currentValue.Equals(_prevValue) && onValueChange != null)
            {
                onValueChange?.Invoke(currentValue, originValue, _prevValue);
            }
            _prevValue = currentValue;
            _isDirty = false;
            return currentValue;
        }

        /// <summary>
        /// Gets the current value.
        /// 获取当前值。
        /// </summary>
        public T value => GetCurrent();

        /// <summary>
        /// Returns the current value, computing it if it is marked dirty.
        /// 返回当前值，如果标记为脏就进行计算。
        /// </summary>
        public T GetCurrent() { return _isDirty ? Calculate() : currentValue; }

        /// <summary>
        /// Gets the origin value.
        /// 获取原始值。
        /// </summary>
        public T GetOrigin() { return originValue; }

        /// <summary>
        /// Finds the first attribute action that matches the specified criteria.
        /// 找到符合指定条件的第一个属性动作。
        /// </summary>
        /// <param name="match">The criteria to match.
        /// 要匹配的条件。</param>
        public AttributeAction<T> Find(Func<AttributeAction<T>, bool> match)
        {
            return actions.Find(match);
        }

        /// <summary>
        /// Gets the array of actions that match the criteria.
        /// 获取符合条件的动作数组。
        /// </summary>
        /// <param name="match">The criteria to match.
        /// 要匹配的条件。</param>
        public AttributeAction<T>[] GetActions(Func<AttributeAction<T>, bool> match)
        {
            return actions.GetActions(match);
        }

        /// <summary>
        /// Gets the array of actions by tag.
        /// 通过标签获取动作数组。
        /// </summary>
        /// <param name="tag">The tag of the actions.
        /// 动作的标签。</param>
        public AttributeAction<T>[] GetActions(string tag)
        {
            return actions.GetActions(tag);
        }

        /// <summary>
        /// Gets the array of actions by priority.
        /// 根据优先级获取动作数组。
        /// </summary>
        /// <param name="priority">The priority of the actions.
        /// 动作的优先级。</param>
        public AttributeAction<T>[] GetActions(AttributePriority priority)
        {
            return actions.GetActions(priority);
        }

        /// <summary>
        /// Gets the array of actions by action function.
        /// 根据动作函数获取动作数组。
        /// </summary>
        /// <param name="action">The action function.
        /// 动作函数。</param>
        public AttributeAction<T>[] GetActions(AttributeValueChange<T> action)
        {
            return actions.GetActions(action);
        }

        /// <summary>
        /// Inserts a computation action with the specified tag.
        /// 插入计算动作，带指定标签。
        /// </summary>
        /// <param name="tag">The tag for the action.
        /// 动作的标签。</param>
        /// <param name="func">The function representing the action.
        /// 表示动作的函数。</param>
        /// <returns>Whether the insert was successful.
        /// 插入是否成功。</returns>
        public bool Push(string tag, AttributeValueChange<T> func) { return Push(func, AttributePriority.First, tag) != null; }

        /// <summary>
        /// Inserts a computation action with the specified priority and tag.
        /// 插入计算动作，带指定优先级和标签。
        /// </summary>
        /// <param name="func">The function representing the action.
        /// 表示动作的函数。</param>
        /// <param name="priority">The priority of the action.
        /// 动作的优先级。</param>
        /// <param name="tag">The tag for the action.
        /// 动作的标签。</param>
        /// <returns>The inserted action.
        /// 插入的动作。</returns>
        public virtual AttributeAction<T> Push(AttributeValueChange<T> func, AttributePriority priority = AttributePriority.First, string tag = "")
        {
            if (func == null) return null;
            _isDirty = true;
            return actions.Push(func, priority, tag);
        }

        /// <summary>
        /// Removes an action by its tag.
        /// 通过标签移除动作。
        /// </summary>
        /// <param name="tag">The tag of the action to remove.
        /// 要移除的动作的标签。</param>
        public void Remove(string tag)
        {
            actions.Remove(tag);
            _isDirty = true;
        }

        /// <summary>
        /// Removes a specific action.
        /// 移除指定动作。
        /// </summary>
        /// <param name="action">The action to remove.
        /// 要移除的动作。</param>
        public void Remove(AttributeAction<T> action)
        {
            actions.Remove(action);
            _isDirty = true;
        }

        /// <summary>
        /// Removes actions by their action function.
        /// 通过动作函数移除动作。
        /// </summary>
        /// <param name="action">The action function to remove.
        /// 要移除的动作函数。</param>
        public void Remove(AttributeValueChange<T> action)
        {
            actions.Remove(action);
            _isDirty = true;
        }

        /// <summary>
        /// Removes the most recently inserted action.
        /// 移除最近插入的动作。
        /// </summary>
        public void Pop()
        {
            actions.Pop();
            _isDirty = true;
        }

        /// <summary>
        /// Checks if the current value is equal to another value.
        /// 检查当前值是否等于另一个值。
        /// </summary>
        /// <param name="other">The value to compare.
        /// 要比较的值。</param>
        public bool Equals(T other)
        {
            return currentValue?.Equals(other) ?? false;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// 返回表示当前对象的字符串。
        /// </summary>
        public override string ToString()
        {
            return _isDirty ? Calculate().ToString() : currentValue.ToString();
        }
    }
}