using System;

namespace PowerCellStudio
{
    internal sealed class TypeLayout
    {
        public Func<object> CreateInstance;
        public FieldAccessor[] Fields;
    }
}