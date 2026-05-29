using System;

namespace PowerCellStudio
{
    /// <summary>
    /// ISceneFlow中执行的具体操作
    /// </summary>
    public interface IFlowStep : IDisposable
    {
        public string stepName { get; }
        public FlowState state { get; }
        
        /// <summary>
        /// SceneFlow开始转移时会重置step
        /// </summary>
        public void Reset();
        /// <summary>
        /// 步骤开始
        /// </summary>
        /// <param name="context"></param>
        public void Start(IFlowContext context);
        /// <summary>
        /// 步骤等待完成
        /// </summary>
        /// <param name="context"></param>
        /// <param name="deltaTime"></param>
        public void Update(IFlowContext context, float deltaTime);
        /// <summary>
        /// 步骤完成后的执行
        /// </summary>
        /// <param name="context"></param>
        public void Exit(IFlowContext context);
        /// <summary>
        /// 在SceneFlow开始转移完成时调用
        /// </summary>
        /// <param name="context"></param>
        public void OnSceneFlowed(IFlowContext context);
        /// <summary>
        /// 触发步骤失败
        /// </summary>
        /// <param name="context"></param>
        public void Fail(IFlowContext context);
    }
}