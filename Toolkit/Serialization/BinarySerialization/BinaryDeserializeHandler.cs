using System;
using System.IO;
using System.Text;

namespace PowerCellStudio
{
    internal class BinaryDeserializeHandler
    {
        #region 读取核心逻辑

        public static object ReadValue(BinaryReader reader, Type type, Encoding encoding)
        {
            return BinaryFormatterResolver.GetFormatter(type).Read(reader, encoding);
        }

        #endregion
    }
}