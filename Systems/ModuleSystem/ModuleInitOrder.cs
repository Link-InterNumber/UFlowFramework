using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleInitOrder : Attribute
    {
        public int order;
        
        public ModuleInitOrder(int order)
        {
            this.order = order;
        }
    }
}