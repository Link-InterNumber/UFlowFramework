using System;
using System.Collections.Generic;
using System.IO;
using PowerCellStudio;
using ProtoBuf;

namespace GameProtocol
{
    public class NetworkSerializer
    {
        private static readonly int msgIdSize = 4;
        
        // 序列化消息结构： [4字节ID][protobuf数据]
        public static byte[] Serialize<T>(T message) where T : class
        {
            using var stream = new MemoryStream();
            // 写入消息ID
            int msgId = MessageIds.TypeToId(typeof(T));
            byte[] idBytes = BitConverter.GetBytes(msgId);
            stream.Write(idBytes, 0, msgIdSize);
        
            // 写入protobuf数据
            Serializer.Serialize(stream, message);
            return stream.ToArray();
        }

        public static object Deserialize(byte[] data, int size, out Type messageType)
        {
            using var stream = new MemoryStream(data);
            byte[] idBytes = new byte[msgIdSize];
            var bytesRead = stream.Read(idBytes, 0, msgIdSize);
            if (bytesRead != msgIdSize)
            {
                NetWorkLog.LogError("Incomplete message ID");
                messageType = null;
                return null;
            }
            int msgId = BitConverter.ToInt32(idBytes, 0);
            messageType = MessageIds.IdToType(msgId);
            if (messageType == null) {
                NetWorkLog.LogError($"未知消息ID: {msgId}");
                return null;
            }
            var dataBytes = new byte[size - msgIdSize];
            Buffer.BlockCopy(data, 4, dataBytes, 0, size - msgIdSize);
            using var memory = new MemoryStream(dataBytes);
            return Serializer.NonGeneric.Deserialize(messageType, memory);
        }
        

    }
}