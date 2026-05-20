using System;

namespace PowerCellStudio
{
    /// <summary>
    /// 模块依赖关系特性，用于标记模块之间的依赖关系。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModuleDependence : Attribute
    {
        public Type DependModuleType { get; private set; }

        public ModuleDependence(Type dependModuleType)
        {
            DependModuleType = dependModuleType;
        }
    }
}