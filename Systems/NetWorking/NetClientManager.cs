using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using NetCoreServer;
using PowerCellStudio;
using UnityEngine;

namespace GameProtocol
{
    public class NetClientManager : SingletonBase<NetClientManager>, IExecutionModule
    {
        private UnityTcpClient _client;

        #region Net Working Config

        private string _address = "127.0.0.1";
        private int _port = 6000;
        
        [Tooltip("Number of times the message is repeated to simulate more requests.")]
        [SerializeField]
        private int _repeatMessage = 0;
        
        [Tooltip("Try to reconnect if connection could not be established or was lost")]
        [SerializeField]
        private bool _autoTryReconnect = true;
        
        private float _reconnectionDelay = 1.0f;

        #endregion

        private Queue<QueueLog> _logQueue = new Queue<QueueLog>();
        private byte[] _buffer;
        private bool _disconnectingManually;
        public bool IsConnected => _client != null && _client.IsConnected;

        public void OnInit()
        {
            _disconnectingManually = false;
        }

        public void Dispose()
        {
            Disconnect();
            _client?.Dispose();
            _client = null;
        }
        
        [ContextMenu("Connect")]
        public void Connect()
        {
            if (_client != null && (_client.IsConnected || _client.IsConnecting))
            {
                Disconnect();
                _client = null;
            }
            
            _client = new UnityTcpClient(_address, _port);
            _buffer = new byte[_client.OptionReceiveBufferSize];
            
            _client.OnConnectedEvent += OnConnected;
            _client.OnDisconnectedEvent += OnDisconnected;
            _client.OnErrorEvent += OnError;
            _client.ConnectAsync();
        }

        public void SetAutoTryReconnect(bool value)
        {
            _autoTryReconnect = value;
            if (_client != null && !_client.IsConnected && !_client.IsDisposed && _autoTryReconnect)
            {
                ReconnectDelayedAsync();
            }
        }

        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            if (_client == null || _client.IsConnected)
            {
                return;
            }
            ApplicationManager.instance.StartCoroutine(DisconnectHandler());
        }
        
        private IEnumerator DisconnectHandler()
        {
            _disconnectingManually = true;
            _client.Disconnect();
            while (_client.IsConnected)
            {
                yield return null;
            }
            _client.OnConnectedEvent -= OnConnected;
            _client.OnDisconnectedEvent -= OnDisconnected;
            _client.OnErrorEvent -= OnError;
            _disconnectingManually = false;
            _listenerHandlers.Clear();
            _waitHandlers.Clear();
            _sendDataBuffers.Clear();
        }

        private void OnConnected()
        {
            QueueLog(QueueLogLevel.Info, $"{_client.GetType()} connected a session with Id {_client.Id}");
        }

        private void QueueLog(QueueLogLevel logLevel, string message)
        {
            var log = new QueueLog()
            {
                logLevel = logLevel,
                logMessage = message
            };
            _logQueue.Enqueue(log);
        }

        private void DisplayLog()
        {
            while (_logQueue.Count > 0)
            {
                var log = _logQueue.Dequeue();
                switch (log.logLevel)
                {
                    case QueueLogLevel.Info:
                        NetWorkLog.Log(log.logMessage);
                        break;
                    case QueueLogLevel.Warning:
                        NetWorkLog.LogWarning(log.logMessage);
                        break;
                    case QueueLogLevel.Error:
                        NetWorkLog.LogError(log.logMessage);
                        break;
                }
            }
        }

        private void OnDisconnected()
        {
            var log = new QueueLog()
            {
                logLevel = QueueLogLevel.Warning,
                logMessage = $"{_client.GetType()} disconnected a session with Id {_client.Id}"
            };
            _logQueue.Enqueue(log);
            if (ApplicationManager.appState == ApplicationState.Quit)
            {
                return;
            }

            if (_autoTryReconnect && !_disconnectingManually)
            {
                ReconnectDelayedAsync();
            }
        }
        
        private void ReconnectDelayedAsync()
        {
            ApplicationManager.instance.StartCoroutine(ReconnectDelayedAsyncHandler());
        }
        
        private IEnumerator ReconnectDelayedAsyncHandler()
        {
            yield return new WaitForSeconds(_reconnectionDelay);
            if (_client.IsConnected || _client.IsConnecting)
            {
                yield break;
            }
            QueueLog(QueueLogLevel.Warning, "Trying to reconnect");
            _client.ConnectAsync();
        }
        
        private void OnError(SocketError error)
        {
            QueueLog(QueueLogLevel.Error, $"{_client.GetType()} caught an error with code {error}");
        }

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
            }
        }
        
        private Dictionary<Type, IMessageReceiveHandler> _waitHandlers = new Dictionary<Type, IMessageReceiveHandler>();
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

        public void Send<T>(T message)
            where T : class, global::ProtoBuf.IExtensible
        {
            if (_client == null || !_client.IsConnected || message == null)
            {
                return;
            }
            var buffer = NetworkSerializer.Serialize(message);
            SendAsync(buffer);
        }
        
        private List<SendDataBuffer> _sendDataBuffers = new List<SendDataBuffer>();
        public MessageReceiveHandler<T> SendQueue<T, TK>(TK message) 
            where T : class, global::ProtoBuf.IExtensible
            where TK : class, global::ProtoBuf.IExtensible
        {
            if (_client == null || !_client.IsConnected || message == null)
            {
                return null;
            }
            var buffer = NetworkSerializer.Serialize(message);
            var handler = new MessageReceiveHandler<T>(true);
            var messageType = typeof(T);
            if (_waitHandlers.ContainsKey(messageType))
            {
                _sendDataBuffers.Add(new SendDataBuffer(messageType, buffer, handler));
                return handler;
            }
            _waitHandlers.Add(messageType, handler);
            SendAsync(buffer);
            return handler;
        }

        public MessageReceiveHandler<T> GetWaitHandler<T>()
            where T : class, global::ProtoBuf.IExtensible
        {
            var messageType = typeof(T);
            if(_waitHandlers.TryGetValue(messageType, out var handler))
            {
                return handler as MessageReceiveHandler<T>;
            }
            var messageReceiveHandler = new MessageReceiveHandler<T>(true);
            _waitHandlers.Add(messageType, messageReceiveHandler);
            return messageReceiveHandler;
        }

        private void SendAsync(byte[] message)
        {
            for (int i = 0; i < 1 + _repeatMessage; i++)
            {
                _client.SendAsync(message);
            }
        }

        public bool inExecution { get; set; }
        public void Execute(float dt)
        {
            DisplayLog();
            bool connected = _client != null && _client.IsConnected;
            if (!connected)
            {
                return;
            }
            SendQueueBuffer();
            HandleReceivedPackages();
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
                _waitHandlers.Add(sendDataBuffer.MessageType, sendDataBuffer.Handler);
                SendAsync(sendDataBuffer.Buffer);
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
                var message = NetworkSerializer.Deserialize(_buffer, length, out var messageType);
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
    }
}