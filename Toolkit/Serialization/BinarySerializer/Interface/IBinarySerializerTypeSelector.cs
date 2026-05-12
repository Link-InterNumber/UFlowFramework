using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    // 为一些特殊类型提供自定义的序列化处理器选择接口
    public interface IBinarySerializerTypeSelector
    {
        // 目标类型
        Type TargetType { get; }

        // 自定义的写入方法
        void Write(BinaryWriter writer, object value, Encoding encoding);
        
        // 自定义的读取方法
        object Read(BinaryReader reader, Encoding encoding);
    }
}