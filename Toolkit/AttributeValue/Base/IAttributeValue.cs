using System;
using System.Runtime.Serialization;

namespace PowerCellStudio
{
    public interface IAttributeValue<T>: IEquatable<T>, IEquatable<IAttributeValue<T>>, ICloneT<IAttributeValue<T>>
    {
        public AttributeActionContainer<T> GetActions();
        public bool ValueEquals(IAttributeValue<T> other);
        public void ResetAction();
        public void Set(T newValue);
        public void Reset(T originValue);
        public T Calculate();
        public T GetCurrent();
        public T GetOrigin();
        public bool Push(string tag, Func<T, T, T> func);
        public void Remove(string tag);
    }
}