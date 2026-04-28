using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal class BinarySerializeHandler
    {
        #region 写入核心逻辑

        public static void WriteValue(BinaryWriter writer, object value, Type type, Encoding encoding)
        {
            BinaryFormatterResolver.GetFormatter(type).Write(writer, value, encoding);
        }
        #endregion
    }
}