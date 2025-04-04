using System;
using UnityEngine;

namespace PowerCellStudio
{
    public delegate void AttributeValueEvent<T>(T currentValue, T originValue, T prevValue);
    
    [Serializable]
    public class AttributeValue<T>: IAttributeValue<T>
    {
        public AttributeValue(T initValue)
        {
            Reset(initValue);
        }
        
        [SerializeField] public T originValue;
        [SerializeField] public T currentValue;
        protected T _prevValue;
        [SerializeField] public AttributeActionContainer<T> actions;
        public event AttributeValueEvent<T> onValueChange;

        public AttributeActionContainer<T> GetActions(){return actions;}

        public bool Equals(IAttributeValue<T> other)
        {
            return originValue.Equals(other.GetOrigin()) && currentValue.Equals(other.GetCurrent()) && actions.Count == other.GetActions().Count;
        }

        public bool ValueEquals(IAttributeValue<T> other) { return originValue.Equals(other.GetOrigin()) && currentValue.Equals(other.GetCurrent()); }

        public IAttributeValue<T> Clone()
        {
            var cloned = new AttributeValue<T>(originValue);
            cloned.actions = actions.Clone();
            return cloned;
        }

        public void ResetAction()
        {
            actions.Clear();
            // Calculate();
            _prevValue = currentValue;
            currentValue = originValue;
        }

        public void Reset(T initValue)
        {
            originValue = initValue;
            _prevValue = initValue;
            currentValue = initValue;
            actions = new AttributeActionContainer<T>();
            // Calculate();
        }

        public void Set(T newValue)
        {
            originValue = newValue;
            Calculate();
        }

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
            return currentValue;
        }
        
        public T value => GetCurrent();

        public T GetCurrent() { return currentValue; }

        public T GetOrigin() { return originValue; }
        
        public AttributeAction<T> Find(Func<AttributeAction<T>, bool> match)
        {
            return actions.Find(match);
        }
        
        public AttributeAction<T>[] GetActions(Func<AttributeAction<T>, bool> match)
        {
            return actions.GetActions(match);
        }
        
        public AttributeAction<T>[] GetActions(string tag)
        {
            return actions.GetActions(tag);
        }
        
        public AttributeAction<T>[] GetActions(AttributePriority priority)
        {
            return actions.GetActions(priority);
        }

        public AttributeAction<T>[] GetActions(Func<T, T, T> action)
        {
            return actions.GetActions(action);
        }

        /// <summary>
        /// 插入计算式
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="func">当前值，原始值，返回值</param>
        /// <returns></returns>
        public bool Push(string tag, Func<T, T, T> func) { return Push(func, AttributePriority.First, tag) != null; }

        /// <summary>
        /// 插入计算式
        /// </summary>
        /// <param name="func">当前值，原始值，返回值</param>
        /// <param name="priority">优先级</param>
        /// <param name="tag">标签</param>
        /// <returns></returns>
        public AttributeAction<T> Push(Func<T, T, T> func, AttributePriority priority = AttributePriority.First, string tag = "")
        {
            if (func == null) return null;
            return actions.Push(func, priority, tag);
        }

        public void Remove(string tag) { actions.Remove(tag); }
        
        public void Remove(AttributeAction<T> action) { actions.Remove(action); }
        
        public void Pop() { actions.Pop(); }
        public bool Equals(T other)
        {
            return currentValue?.Equals(other)?? false;
        }

        public override string ToString()
        {
            return currentValue.ToString();
        }
    }
}