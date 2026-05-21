using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class PersistenceMigrationMethodAttribute : Attribute
    {
        public int fromVersion { get; }
        public int toVersion { get; }

        public PersistenceMigrationMethodAttribute(int fromVersion, int toVersion)
        {
            this.fromVersion = Math.Max(0, fromVersion);
            this.toVersion = Math.Max(this.fromVersion + 1, toVersion);
        }
    }
}