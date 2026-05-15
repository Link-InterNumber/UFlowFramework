using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public partial class NetClientManager
    {
#if NET_TEST
        private int _repeatMessage = 1;
#endif

#region 消息监听注册

        private Dictionary<Type, IMessageReceiveHandler> _listenerHandlers = new Dictionary<Type, IMessageReceiveHandler>();

        public void AddMessageListener<T>(BaseLinkAction<T> onReceived)
            where T : class, global::ProtoBuf.IExtensible
        {
            _listenerHandlers.TryGetValue(typeof(T), out var handler);
            if(handler == null)
            {
                handler = new MessageReceiveHandler<T>();
                _listenerHandlers.Add(typeof(T), handler);
            }
            var messageReceiveHandler = handler as MessageReceiveHandler<T>;
            messageReceiveHandler?.AddListener(onReceived);
        }
        
        public void RemoveMessageListener<T>(BaseLinkAction<T> onReceived)
            where T : class, global::ProtoBuf.IExtensible
        {
            _listenerHandlers.TryGetValue(typeof(T), out var handler);
            if(handler == null)
            {
                return;
            }
            var messageReceiveHandler = handler as MessageReceiveHandler<T>;
            if (messageReceiveHandler == null) return;
            messageReceiveHandler.RemoveListener(onReceived);
            if(messageReceiveHandler.EventListenerCount == 0)
            {
                _listenerHandlers.Remove(typeof(T));
                messageReceiveHandler.Dispose();
            }
        }

#endregion

#region 消息发送

        private bool SendAsync(byte[] message)
        {
#if NET_TEST
            for (int i = 0; i < 1 + _repeatMessage; i++)
#endif
            {
                var success = _client.SendAsync(message);
                if (!success) return false;
            }
            return true;
        }

        private class SendDataBuffer : IDisposable
        {
            private byte[] _buffer;
            public byte[] buffer => _buffer;
            private IMessageReceiveHandler _handler;
            public IMessageReceiveHandler handler => _handler;
            private Type _messageType;
            public Type messageType => _messageType;

            private Type _respondType;
            public Type respondType => _respondType;

            public SendDataBuffer(Type messageType, Type respondType, byte[] buffer, IMessageReceiveHandler  handler)
            {
                _buffer = buffer;
                _handler = handler;
                _messageType = messageType;
                _respondType = respondType;
            }

            public void Dispose()
            {
                _buffer = null;
                _handler = null;
                _messageType = null;
                _respondType = null;
            }
        }

        private List<SendDataBuffer> _sendDataBuffers = new List<SendDataBuffer>();
        private HashSet<Type> _queuedMessages = new HashSet<Type>();
        private Dictionary<Type, (IMessageReceiveHandler, Type)> _waitHandlers = new Dictionary<Type, (IMessageReceiveHandler, Type)>();

        public void Send<T>(T message)
        {
            if (_client == null || !_client.IsConnected || message == null)
            {
                return;
            }
            var buffer = _networkSerializer.Serialize(message);
            SendAsync(buffer);
        }
        
        public MessageReceiveHandler<TRespond> SendQueue<TRequest, TRespond>(TRequest message) 
        {
            if (_client == null || !_client.IsConnected || message == null)
            {
                return null;
            }
            var messageType = typeof(TRequest);
            var respondType = typeof(TRespond);
            var buffer = _networkSerializer.Serialize(message);
            var handler = new MessageReceiveHandler<TRespond>(true);
            _sendDataBuffers.Add(new SendDataBuffer(messageType, respondType, buffer, handler));
            return handler;
        }

        private void SendQueueBuffer()
        {
            if (_sendDataBuffers.Count <= 0) return;
            for (var i = 0; i < _sendDataBuffers.Count;)
            {
                var sendDataBuffer = _sendDataBuffers[i];
                if (_queuedMessages.Contains(sendDataBuffer.messageType))
                {
                    i++;
                    continue;
                }
                if (!SendAsync(sendDataBuffer.buffer))
                {
                    break;
                }
                _queuedMessages.Add(sendDataBuffer.messageType);
                _waitHandlers[sendDataBuffer.respondType] = (sendDataBuffer.handler, sendDataBuffer.messageType);
                sendDataBuffer.Dispose();
                _sendDataBuffers.RemoveAt(i);
            }
        }

        private void HandleReceivedPackages()
        {
            if (!_client.HasEnqueuedPackages()) return;
            while (_client.HasEnqueuedPackages())
            {
                var length = _client.GetNextPackage(ref _buffer);
                if (length <= 0) continue;
                var message = _networkSerializer.Deserialize(_buffer, length, out var messageType);
                if (message == null) continue;
                if (_listenerHandlers.TryGetValue(messageType, out var handler))
                {
                    handler.OnReceived(message, messageType);
                    if (handler.invokeOnce)
                    {
                        _listenerHandlers.Remove(messageType);
                    }
                }
                if (_waitHandlers.TryGetValue(messageType, out var queueHandler))
                {
                    queueHandler.Item1.OnReceived(message, messageType);
                    _waitHandlers.Remove(messageType);
                    _queuedMessages.Remove(queueHandler.Item2);
                }
            }
        }

#endregion
    }
}