using System;

namespace PowerCellStudio
{
    public interface IMessageIdMap
    {
        int TypeToId(Type messageType);

        Type IdToType(int messageId);
    }
}