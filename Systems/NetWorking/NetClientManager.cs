using System.Collections;
using System.Net.Sockets;
using NetCoreServer;
using PowerCellStudio;
using UnityEngine;

namespace GameProtocol
{
    public partial class NetClientManager : SingletonBase<NetClientManager>, IExecutionModule
    {
        private UnityTcpClient _client;

        #region Net Working Config

        private string _address = "127.0.0.1";
        private int _port = 6000;
        
        [Tooltip("Try to reconnect if connection could not be established or was lost")]
        [SerializeField]
        private bool _autoTryReconnect = true;
        
        private float _reconnectionDelay = 1.0f;

        #endregion

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
            _client.OnConnectedEvent -= OnConnected;
            _client.OnDisconnectedEvent -= OnDisconnected;
            _client.OnErrorEvent -= OnError;
            _client.Disconnect();
            while (_client.IsConnected)
            {
                yield return null;
            }
            _disconnectingManually = false;
        }

        private void OnConnected()
        {
            AppendLog(QueueLogLevel.Info, $"{_client.GetType()} connected a session with Id {_client.Id}");
            EventManager.instance?.onNetConnect?.Invoke();
        }

        private void OnDisconnected()
        {
            var log = new QueueLog()
            {
                logLevel = QueueLogLevel.Warning,
                logMessage = $"{_client.GetType()} disconnected a session with Id {_client.Id}"
            };
            _logQueue.Enqueue(log);
            EventManager.instance?.onNetDisconnect?.Invoke();
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
            AppendLog(QueueLogLevel.Warning, "Trying to reconnect");
            _client.ConnectAsync();
        }
        
        private void OnError(SocketError error)
        {
            AppendLog(QueueLogLevel.Error, $"{_client.GetType()} caught an error with code {error}");
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
    }
}