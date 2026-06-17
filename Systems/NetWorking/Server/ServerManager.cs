// ServerManager.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;

namespace PowerCellStudio
{
    public class ServerManager : MonoBehaviour
    {
        private UnityTcpServer _server;
        private INetworkSerializer _serializer;
        

        void StartServer<T>(T networkSerializer) where T : INetworkSerializer
        {
            _serializer = networkSerializer;
            // 在本地启动端口6000
            _server = new UnityTcpServer(IPAddress.Any, 6000);
            _server.OnConnectedEvent += (id) => QueueLog(QueueLogLevel.Info, $"Client {id} connected");
            _server.OnDisconnectedEvent += (id) => QueueLog(QueueLogLevel.Warning, $"Client {id} disconnected");
            _server.OnErrorEvent += error => QueueLog(QueueLogLevel.Error, $"Server error: {error}");
            _server.Start();
            QueueLog(QueueLogLevel.Info, $"Server started on port 6000");
        }

        void OnDestroy()
        {
            _server?.Dispose();
        }

        private Queue<QueueLog> _logQueue = new Queue<QueueLog>();

        private void QueueLog(QueueLogLevel logLevel, string message, StackTrace stackTrace = null)
        {
            _logQueue.Enqueue(new QueueLog()
            {
                logLevel = logLevel,
                logMessage = message,
                callStack = stackTrace?.ToString() ?? null
            });
        }

        public void Send<T>(Guid id, T message)
        {
            if (message == null) return;
            var session = _server.FindSession(id);
            if (session == null) return;
            var buffer = _serializer.Serialize(message);
            session.SendAsync(buffer);
        }

        public void Broadcast<T>(T message) where T : INetworkSerializer
        {
            if (message == null) return;
            var buffer = _serializer.Serialize(message);
            _server.Multicast(buffer);
        }

        private void Update()
        {
            if (_server == null || !_server.IsStarted || _server.IsDisposed) return;
            while (_logQueue.Count > 0)
            {
                var log = _logQueue.Dequeue();
                var message = log.callStack != null ? $"{log.logMessage}\n{log.callStack}" : log.logMessage;
                switch (log.logLevel)
                {
                    case QueueLogLevel.Info:
                        NetWorkLogger.Log(message);
                        break;
                    case QueueLogLevel.Warning:
                        NetWorkLogger.LogWarning(message);
                        break;
                    case QueueLogLevel.Error:
                        NetWorkLogger.LogError(message);
                        break;
                }
            }

            if (_server == null || !_server.IsStarted || _server.IsDisposed)
            {
                return;
            }

            _server.Update(_serializer);
        }

        [TestButton]
        public void TestConnect()
        {
            NetClientManager.instance.Connect(_serializer);
        }

        // [TestButton]
        // public void TestClientSend()
        // {
        //     NetClientManager.instance.RemoveMessageListener<ServerResponse>(OnResiceveMessage);
        //     var playmove = new PlayerMove();
        //     playmove.X = 1;
        //     playmove.Y = 2;
        //     playmove.Z = 3;
        //     NetClientManager.instance.AddMessageListener<ServerResponse>(OnResiceveMessage);
        //     NetClientManager.instance.Send(playmove);
        // }
        //
        // private void OnResiceveMessage(ServerResponse data)
        // {
        //     NetClientManager.instance.RemoveMessageListener<ServerResponse>(OnResiceveMessage);
        //     QueueLog(QueueLogLevel.Info, $"OnResiceveMessage: Received response: {data.Success}", new StackTrace(true));
        // }
        //
        //
        // [TestButton]
        // public void TestClientSendQueue()
        // {
        //     var playmove = new PlayerMove();
        //     playmove.X = 1;
        //     playmove.Y = 2;
        //     playmove.Z = 3;
        //     var handler = NetClientManager.instance.SendQueue<ServerResponse, PlayerMove>(playmove);
        //     handler.AddListener(response =>
        //     {
        //         QueueLog(QueueLogLevel.Info, $"TestClientSendQueue: Received response: {response.Success}", new StackTrace(true));
        //         handler.GetMessage();
        //     });
        // }
    }
}