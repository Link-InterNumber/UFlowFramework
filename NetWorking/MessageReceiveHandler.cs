using System;
using System.Collections;
using PowerCellStudio;
using UnityEngine;

namespace GameProtocol
{
    public interface IMessageReceiveHandler : IDisposable, IEnumerator
    {
        public void OnReceived(object message, Type messageType);
    }
    
    public class MessageReceiveHandler<T> : CustomYieldInstruction, IMessageReceiveHandler
        where T : class, global::ProtoBuf.IExtensible
    {
        private bool _invokeOnce = false;
        
        public MessageReceiveHandler(bool invokeOnce = false)
        {
            _invokeOnce = invokeOnce;
        }

        public void OnReceived(object message, Type messageType)
        {
            if (messageType != typeof(T)) return;
            ReceiveMessage(message as T);
            if (!_invokeOnce) return;
            RemoveAllListeners();
        }

        private LinkEvent<T> _onreceivedEvent = new LinkEvent<T>();
        
        private T _message;
        public T GetMessage()
        {
            return _message;
        }
        
        public void AddListener(BaseLinkAction<T> action)
        {
            _onreceivedEvent.AddListener(action);
        }
        
        public void RemoveListener(BaseLinkAction<T> action)
        {
            _onreceivedEvent.RemoveListener(action);
        }
        
        public void RemoveAllListeners()
        {
            _onreceivedEvent.RemoveAllListeners();
        }
        
        private void ReceiveMessage(T message)
        {
            _message = message;
            _onreceivedEvent.Invoke(message);
        }

        public override bool keepWaiting { get => _message == null; }
        public void Dispose()
        {
            RemoveAllListeners();
            _onreceivedEvent = null;
            _message = null;
        }

        public int EventListenerCount => _onreceivedEvent?.GetEventListenerCount()??0;
    }
}