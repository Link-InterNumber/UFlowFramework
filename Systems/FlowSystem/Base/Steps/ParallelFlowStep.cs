using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class ParallelFlowStep : FlowStepBase
    {
        private FlowStepProcessor _children = new FlowStepProcessor();

        public ParallelFlowStep(string stepName = null, params IFlowStep[] children) : base(stepName)
        {
            if (children == null) return;
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] == null) continue;
                _children.AddStep(children[i]);
            }
        }

        public void AddChild(IFlowStep step)
        {
            if (step == null) return;
            _children.AddStep(step);
        }

        protected override void OnReset()
        {
            _children.Reset();
        }

        protected override void OnStart(IFlowContext context)
        {
            _children.ChainUpdate(context, 0f);
        }

        protected override void OnUpdate(IFlowContext context, float deltaTime)
        {
            if (state == FlowState.Completed || state == FlowState.Failed) return;
            _children.ParallelUpdate(context, deltaTime);
            if (_children.flowState == FlowState.Completed)
            {
                CompleteStep();
            }
            else if (_children.flowState == FlowState.Failed)
            {
                Fail(context);
            }
        }

        protected override void OnExit(IFlowContext context)
        {
            
        }

        public override void Dispose()
        {
            _children.Dispose();
            _children = null;
        }
    }
}