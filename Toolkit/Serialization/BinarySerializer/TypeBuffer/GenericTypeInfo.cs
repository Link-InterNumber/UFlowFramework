using System;

namespace PowerCellStudio
{
    internal struct GenericTypeInfo
    {
        public Type type;
        public Type genericDefinition;
        public Type[] genericArguments;
        public Type resolvedType;
    }
}