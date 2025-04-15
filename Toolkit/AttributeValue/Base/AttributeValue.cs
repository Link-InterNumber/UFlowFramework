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
        
        [SerializeField] protected T originValue;
        [SerializeField] protected T currentValue;
        protected T _prevValue;
        [SerializeField] protected AttributeActionContainer<T> actions;
        public event AttributeValueEvent<T> onValueChange;

        public AttributeActionContainer<T> GetActions(){return actions;}

        private bool _isDirty = true;

        public bool Equals(IAttributeValue<T> other)
        {
            return originValue.Equals(other.GetOrigin()) 
                && GetCurrent().Equals(other.GetCurrent()) 
                && actions.Count == other.GetActions().Count;
        }

        public bool ValueEquals(IAttributeValue<T> other) 
        { 
            return originValue.Equals(other.GetOrigin()) 
                && GetCurrent().Equals(other.GetCurrent());
        }

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
            _isDirty = true;
        }

        public void Reset(T initValue)
        {
            originValue = initValue;
            _prevValue = initValue;
            currentValue = initValue;
            actions = new AttributeActionContainer<T>();
            _isDirty = true;
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
            _isDirty = false;
            return currentValue;
        }
        
        public T value => GetCurrent();

        public T GetCurrent() { return _isDirty ? Calculate() : currentValue; }

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
            _isDirty = true;
            return actions.Push(func, priority, tag);
        }

        public void Remove(string tag) 
        {
            actions.Remove(tag);
            _isDirty = true;
        }
        
        public void Remove(AttributeAction<T> action) 
        {
            actions.Remove(action);
            _isDirty = true;
        }
        
        public void Pop() 
        {
            actions.Pop(); 
            _isDirty = true;
        }

        public bool Equals(T other)
        {
            return currentValue?.Equals(other)?? false;
        }

        public override string ToString()
        {
            return _isDirty? Calculate().ToString() : currentValue.ToString();
        }
    }
}