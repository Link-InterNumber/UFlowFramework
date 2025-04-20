// TcpGameServer.cs

using System;
using System.Collections.Generic;
using System.IO;
using NetCoreServer;
using System.Net;
using System.Net.Sockets;
using DG.Tweening;
using GameProtocol;
using PowerCellStudio;

public class TcpGameServer : TcpServer
{
    /// <inheritdoc />
    public Action OnConnectedEvent;
    /// <inheritdoc />
    public Action OnDisconnectedEvent;
    /// <inheritdoc />
    public Action<SocketError> OnErrorEvent;
    
    private byte[] _buffer;


    public TcpGameServer(IPAddress address, int port) : base(address, port)
    {
        _buffer = new byte[OptionReceiveBufferSize];
    }

    protected override TcpSession CreateSession() => new GameSession(this);

    protected override void OnError(SocketError error)
    {
        OnErrorEvent?.Invoke(error);
    }
    
    public void Update()
    {
        foreach (var session in Sessions.Values)
        {
            var gameSession = (GameSession) session;
            if (!gameSession.HasEnqueuedPackages()) continue;
            var size = gameSession.GetNextPackage(ref _buffer);
            if (size <= 0) continue;
            var message = NetworkSerializer.Deserialize(_buffer, size, out var messageType);
            NetWorkLog.Log($"Received from {session.Id}: {message}");
            DealWithSR(messageType, gameSession);
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

public class GameSession : TcpSession
{
    private MemoryStream queueBuffer;
    private Queue<BufferPointer> queueBufferPointer;
    
    public GameSession(TcpServer server) : base(server)
    {
        queueBuffer = new MemoryStream(OptionReceiveBufferSize);
        queueBufferPointer = new Queue<BufferPointer>();
    }
    
    public bool HasEnqueuedPackages()
    {
        return queueBufferPointer.Count > 0;
    }
    
    public int GetNextPackage(ref byte[] array)
    {
        if (queueBufferPointer.Count == 0)
        {
            return -1;
        }

        var pointer = queueBufferPointer.Dequeue();
        var lastPosition = queueBuffer.Position;
        queueBuffer.Position = pointer.Offset;
        queueBuffer.Read(array, 0, pointer.Length);

        if (queueBufferPointer.Count == 0)
        {
            // All packages read, clear memory stream
            queueBuffer.SetLength(0L);
        }
        else
        {
            queueBuffer.Position = lastPosition;
        }

        return pointer.Length;
    }

    protected override void OnDisconnected()
    {
        base.OnDisconnected();
        var gameServer = (TcpGameServer)Server;
        gameServer.OnDisconnectedEvent?.Invoke();
    }

    protected override void OnConnected()
    {
        var gameServer = (TcpGameServer)Server;
        gameServer.OnConnectedEvent?.Invoke();
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        var start = (int) queueBuffer.Length;
        queueBuffer.Write(buffer, (int) offset, (int) size);
        queueBufferPointer.Enqueue(new BufferPointer(start, (int) size));
        
        // Continue receive datagrams
        ReceiveAsync();
    }
}