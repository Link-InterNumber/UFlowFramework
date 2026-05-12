using System;

namespace PowerCellStudio
{
    // 定义一个接口，表示一个数据块的序列化器
    public interface IChunkSerializer
    {
        // 将数据对象写入到二进制流中
        byte[] Write<T>(T data);

        // 从二进制流中读取数据对象
        T Read<T>(byte[] bytes, int offset, int count);
    }
}