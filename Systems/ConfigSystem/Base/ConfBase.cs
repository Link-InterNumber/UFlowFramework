using System;

namespace PowerCellStudio
{
    [Serializable]
    public class ConfBase
    {
        public ConfBase()
        {
            
        }
        
        public static implicit operator bool(ConfBase conf)
        {
            return conf != null;
        }
    }
    
}