using System;
using System.Collections.Generic;
using PowerCellStudio;

namespace GameProtocol
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
            public byte[] Buffer => _buffer;
            private IMessageReceiveHandler _handler;
            public IMessageReceiveHandler Handler => _handler;
            private Type _messageType;
            public Type MessageType => _messageType;

            public SendDataBuffer(Type messageType, byte[] buffer, IMessageReceiveHandler handler)
            {
                _buffer = buffer;
                _handler = handler;
                _messageType = messageType;
            }

            public void Dispose()
            {
                _buffer = null;
                _handler = null;
                _messageType = null;
            }
        }

        private List<SendDataBuffer> _sendDataBuffers = new List<SendDataBuffer>();

        private Dictionary<Type, IMessageReceiveHandler> _waitHandlers = new Dictionary<Type, IMessageReceiveHandler>();

        public void Send<T>(T message)
            where T : class, global::ProtoBuf.IExtensible
        {
            if (_client == null || !_client.IsConnected || message == null)
            {
                return;
            }
            var buffer = _networkSerializer.Serialize(message);
            SendAsync(buffer);
        }
        
        public MessageReceiveHandler<T> SendQueue<T, TK>(TK message) 
            where T : class, global::ProtoBuf.IExtensible
            where TK : class, global::ProtoBuf.IExtensible
        {
            if (_client == null || !_client.IsConnected || message == null)
            {
                return null;
            }
            var buffer = _networkSerializer.Serialize(message);
            var handler = new MessageReceiveHandler<T>(true);
            var messageType = typeof(T);
            _sendDataBuffers.Add(new SendDataBuffer(messageType, buffer, handler));
            return handler;
        }

        private void SendQueueBuffer()
        {
            if (_sendDataBuffers.Count <= 0) return;
            for (var i = 0; i < _sendDataBuffers.Count;)
            {
                var sendDataBuffer = _sendDataBuffers[i];
                if (_waitHandlers.ContainsKey(sendDataBuffer.MessageType))
                {
                    i++;
                    continue;
                }
                if (!SendAsync(sendDataBuffer.Buffer))
                {
                    break;
                }
                _waitHandlers.Add(sendDataBuffer.MessageType, sendDataBuffer.Handler);
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
                }

                if (_waitHandlers.TryGetValue(messageType, out var waitHandler))
                {
                    waitHandler.OnReceived(message, messageType);
                    _waitHandlers.Remove(messageType);
                }
            }
        }

#endregion
    }
}