using System;
using System.Runtime.CompilerServices;

namespace PowerCellStudio
{
    internal static class EnumUnsafeCaster<TEnum, TUnderlying>
        where TEnum : struct, Enum
        where TUnderlying : struct
    {
        public static TUnderlying Cast(TEnum value)
        {
            return Unsafe.As<TEnum, TUnderlying>(ref value);
        }
    }
}