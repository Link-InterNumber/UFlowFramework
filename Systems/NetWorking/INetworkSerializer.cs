using System;

namespace GameProtocol
{
    public interface INetworkSerializer : IDisposable
    {
        public byte[] Serialize<T>(T message) where T : class;
        public object Deserialize(byte[] data, int size, out Type messageType);
    }
}