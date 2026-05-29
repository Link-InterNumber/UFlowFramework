using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    /// <summary>
    /// Flow系统中的场景，主要负责场景进出时的流程
    /// </summary>
    public interface ISceneFlow : IDisposable
    {
        public string flowName { get; }
        public int id { get; }
        public FlowState inState { get; }
        public FlowState outState { get; }
        public void AddStep(IFlowStep step);
        public void AddTransitionStep(IFlowStep step);
        public void Reset();
        
        /// <summary>
        /// 启动进入场景的操作流
        /// </summary>
        /// <param name="context"></param>
        public void StartFlow(IFlowContext context);
        
        /// <summary>
        /// 检查进入场景的操作流是否完成
        /// </summary>
        /// <param name="context"></param>
        /// <param name="deltaTime"></param>
        public void UpdateFlow(IFlowContext context, float deltaTime);
        
        /// <summary>
        /// 启动退出场景的操作流
        /// </summary>
        /// <param name="context"></param>
        public void StartTransition(IFlowContext context);
        
        /// <summary>
        /// 检查退出场景的操作流是否完成
        /// </summary>
        /// <param name="context"></param>
        /// <param name="deltaTime"></param>
        public void UpdateTransition(IFlowContext context, float deltaTime);
        
        /// <summary>
        /// 退出场景时触发
        /// </summary>
        /// <param name="context"></param>
        public void OnExit(IFlowContext context);
    }
}