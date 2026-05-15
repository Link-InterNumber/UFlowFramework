// TcpGameServer.cs

using System;
using System.Collections.Generic;
using System.IO;
using NetCoreServer;
using System.Net;
using System.Net.Sockets;
using PowerCellStudio;

namespace PowerCellStudio
{
    public class UnityTcpServer : TcpServer
    {
        /// <inheritdoc />
        public Action<Guid> OnConnectedEvent;

        /// <inheritdoc />
        public Action<Guid> OnDisconnectedEvent;

        /// <inheritdoc />
        public Action<SocketError> OnErrorEvent;

        private byte[] _buffer;
        
        public UnityTcpServer(IPAddress address, int port) : base(address, port)
        {
            _buffer = new byte[OptionReceiveBufferSize];
        }

        protected override TcpSession CreateSession() => new GameSession(this);

        protected override void OnError(SocketError error)
        {
            OnErrorEvent?.Invoke(error);
        }

        public void Update(INetworkSerializer serializer)
        {
            foreach (var session in Sessions.Values)
            {
                var gameSession = (GameSession)session;
                while (gameSession.HasEnqueuedPackages())
                {
                    var size = gameSession.GetNextPackage(ref _buffer);
                    if (size <= 0) continue;
                    var message = serializer.Deserialize(_buffer, size, out var messageType);
                    NetWorkLog.Log($"Received from {session.Id}: {message}");
                    DealWithSR(messageType, gameSession);
                }
            }
        }

        
        private static void DealWithSR(Type messageType, GameSession gameSession)
        {
            // if(messageType == typeof(PlayerMove))
            // {
            //     DOVirtual.DelayedCall(Randomizer.Range(0.1f, 0.3f), () =>
            //     {
            //         var ServerResponse = new ServerResponse();
            //         ServerResponse.Success = true;
            //         var messageBuffer = NetworkSerializer.Serialize(ServerResponse);
            //         gameSession.SendAsync(messageBuffer);
            //     });
            // }
        }
    }
}