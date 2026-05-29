using System;

namespace PowerCellStudio
{
    /// <summary>
    /// 只负责记录ISceneFlow和ISceneFlow转换条件/方向
    /// </summary>
    public interface IFlowRoadMap : IDisposable
    {
        public IFlowContext context { get; }
        public string roadMapName { get; }
        public ISceneFlow entryFlow { get; }

        public FlowRoadMap AddFlow(ISceneFlow flow, bool asEntry = false);

        public IFlowRoadMap AddTransition(ISceneFlow from, ISceneFlow to, Func<IFlowContext, bool> condition = null,
            int priority = 0);
        
        public bool TryGetTransition(ISceneFlow from, out ISceneFlow transition);
        
        public ISceneFlow GetSceneFlow(int id);
    }

    // public struct FlowTransition
    // {
    //     public ISceneFlow flow;
    //     public IFlowRoadMap roadMap;
    // }
}