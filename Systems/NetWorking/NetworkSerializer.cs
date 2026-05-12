using System;
using System.Collections.Generic;
using System.IO;
using PowerCellStudio;
using ProtoBuf;

namespace GameProtocol
{
    public class NetworkSerializer : INetworkSerializer
    {
        private static readonly int msgIdSize = sizeof(int);
        private IMessageIdMap _messageIdMap;

        public NetworkSerializer(IMessageIdMap messageIdMap)
        {
            _messageIdMap = messageIdMap;
        }
        
        // 序列化消息结构： [4字节ID][protobuf数据]
        public byte[] Serialize<T>(T message) where T : class
        {
            int msgId = _messageIdMap.TypeToId(typeof(T));
            using var stream = new MemoryStream();
            Span<byte> idSpan = stackalloc byte[msgIdSize];
            BitConverter.TryWriteBytes(idSpan, msgId);
            stream.Write(idSpan);
            Serializer.Serialize(stream, message);
            return stream.ToArray();
        }

        public object Deserialize(byte[] data, int size, out Type messageType)
        {
            // using var stream = new MemoryStream(data);
            // byte[] idBytes = new byte[msgIdSize];
            // var bytesRead = stream.Read(idBytes, 0, msgIdSize);
            // if (bytesRead != msgIdSize)
            if (size < msgIdSize)
            {
                NetWorkLog.LogError("Incomplete message ID");
                messageType = null;
                return null;
            }
            int msgId = BitConverter.ToInt32(data, 0);
            messageType = _messageIdMap.IdToType(msgId);
            if (messageType == null) {
                NetWorkLog.LogError($"未知消息ID: {msgId}");
                return null;
            }
            // 直接用原始data，跳过前4字节，避免分配和拷贝
            using var memory = new MemoryStream(data, msgIdSize, size - msgIdSize, false);
            return Serializer.NonGeneric.Deserialize(messageType, memory);
        }
        
        public void Dispose()
        {
            _messageIdMap = null;
        }
    }
}