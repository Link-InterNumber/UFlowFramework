using System;

#if !UNITY_WEBGL
using System.Threading.Tasks;
#endif

namespace PowerCellStudio
{
    public interface ILoaderYieldInstruction : IDisposable
    {
        public bool isDone { get; }
    }
}