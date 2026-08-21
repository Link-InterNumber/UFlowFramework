using System;

namespace PowerCellStudio.Editor
{
    [Flags]
    public enum DefectLevel
    {
        None = 0,
        Low = 1 << 0,
        Medium = 1 << 1,
        High = 1 << 2,
    }
}