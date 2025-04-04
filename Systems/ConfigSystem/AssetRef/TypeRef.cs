using System;

namespace PowerCellStudio
{
    [Serializable]
    public abstract class TypeRef
    {
        public abstract bool isMatch(string lowerRawType);
        
        public abstract string TypeName();
    }
}