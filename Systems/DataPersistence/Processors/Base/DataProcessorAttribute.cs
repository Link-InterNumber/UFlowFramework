using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DataProcessorAttribute : Attribute
    {
        public PlayerDataType DataType { get; }

        public DataProcessorAttribute(PlayerDataType dataType)
        {
            DataType = dataType;
        }
    }
}