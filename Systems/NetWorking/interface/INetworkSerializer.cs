using System;

namespace PowerCellStudio
{
    public interface INetworkSerializer : IDisposable
    {
        public IMessageIdMap messageIdMap { get; }
        public byte[] Serialize<T>(T message);
        public object Deserialize(byte[] data, int size, out Type messageType); 
    }
}