using System.Collections.Generic;
using System.IO;
using NetCoreServer;

namespace PowerCellStudio
{
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
            var gameServer = (UnityTcpServer)Server;
            gameServer.OnDisconnectedEvent?.Invoke(Id);
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            var gameServer = (UnityTcpServer)Server;
            gameServer.OnConnectedEvent?.Invoke(Id);
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            base.OnReceived(buffer, offset, size);
            var start = (int)queueBuffer.Length;
            queueBuffer.Write(buffer, (int)offset, (int)size);
            queueBufferPointer.Enqueue(new BufferPointer(start, (int)size));

            // Continue receive datagrams
            ReceiveAsync();
        }
    }
}