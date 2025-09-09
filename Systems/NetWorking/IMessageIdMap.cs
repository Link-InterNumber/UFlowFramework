using System;

namespace GameProtocol
{
    public interface IMessageIdMap
    {
        int TypeToId(Type messageType);

        Type IdToType(int messageId);
    }
}