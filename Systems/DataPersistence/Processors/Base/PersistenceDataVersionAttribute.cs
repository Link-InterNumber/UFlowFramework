using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class PersistenceDataVersionAttribute : Attribute
    {
        public int version { get; }

        public PersistenceDataVersionAttribute(int version)
        {
            this.version = Math.Max(1, version);
        }
    }
}