using System;

namespace PowerCellStudio
{
    public abstract class FlowStepBase : IFlowStep
    {
        protected FlowStepBase(string stepName = null)
        {
            this.stepName = string.IsNullOrEmpty(stepName) ? GetType().Name : stepName;
        }

        public string stepName { get; }

        public FlowState state { get; protected set; } = FlowState.NotStarted;

        public void Reset()
        {
            state = FlowState.NotStarted;
            OnReset();
        }

        public void Start(IFlowContext context)
        {
            if (state == FlowState.Running) return;
            state = FlowState.Running;
            OnStart(context);
        }

        public void Update(IFlowContext context, float deltaTime)
        {
            if (state == FlowState.Completed || state == FlowState.Failed) return;
            OnUpdate(context, deltaTime);
        }

        public void Exit(IFlowContext context)
        {
            OnExit(context);
        }
        
        public void Fail(IFlowContext context)
        {
            state = FlowState.Failed;
            OnFail(context);
        }

        public virtual void Dispose()
        {
        }

        protected void CompleteStep()
        {
            state = FlowState.Completed;
        }

        protected virtual void OnReset()
        {
        }

        protected abstract void OnStart(IFlowContext context);

        protected virtual void OnUpdate(IFlowContext context, float deltaTime)
        {
        }

        protected virtual void OnExit(IFlowContext context)
        {
        }

        protected virtual void OnFail(IFlowContext context)
        {
            
        }

        public virtual void OnSceneFlowed(IFlowContext context)
        {
            
        }
    }
}