using System;

namespace PowerCellStudio
{
    [Serializable]
    public abstract class LocalizationRef<T> : TypeRef
    {
        public string rawString;
        public string localizationKey;

        public abstract T Get();
    }
}