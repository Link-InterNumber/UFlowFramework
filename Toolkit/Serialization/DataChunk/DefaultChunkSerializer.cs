using System;
using System.Buffers;

namespace PowerCellStudio
{
    public sealed class DefaultChunkSerializer : IChunkSerializer
    {
        public static DefaultChunkSerializer Instance { get; } = new DefaultChunkSerializer();

        private DefaultChunkSerializer()
        {
        }

        public byte[] Write<T>(T data)
        {
            return BinarySerializer.Serialize(data);
        }

        public T Read<T>(byte[] bytes, int offset, int count)
        {
            return BinarySerializer.Deserialize<T>(bytes, offset, count);
        }
    }
}