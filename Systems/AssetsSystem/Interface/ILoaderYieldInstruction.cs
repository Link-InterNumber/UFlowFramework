using System;

namespace PowerCellStudio
{
    public interface ILoaderYieldInstruction : IDisposable
    {
        public bool isDone { get; }
        public bool autoRelease { get; }
    }
    
    public interface ILoaderYieldInstruction<T> : ILoaderYieldInstruction
    {
        
    }
}