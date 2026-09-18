using System;

namespace UFlowFramework
{
    public interface IInputPage<TKey> : IDisposable
    {
        public bool isEmpty { get; }
        
        public void Handle(in InputEvent<TKey> input);

        public bool HasListener(TKey actionKey);

        public void AddListener(TKey actionKey, Action<InputEvent<TKey>> callback);

        public void RemoveListener(TKey actionKey, Action<InputEvent<TKey>> callback);

        public void RemoveAllListener(TKey actionKey);

        public void ClearAll();

    }
}