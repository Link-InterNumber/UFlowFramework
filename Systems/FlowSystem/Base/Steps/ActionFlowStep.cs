using System;

namespace PowerCellStudio
{
    public class ActionFlowStep : FlowStepBase
    {
        private readonly Action<IFlowContext> _executeAction;
        private readonly Action<IFlowContext> _exitAction;

        public ActionFlowStep(Action<IFlowContext> executeAction, string stepName = null, Action<IFlowContext> exitAction = null)
            : base(stepName)
        {
            _executeAction = executeAction;
            _exitAction = exitAction;
        }

        protected override void OnStart(IFlowContext context)
        {
            _executeAction?.Invoke(context);
            CompleteStep();
        }

        protected override void OnExit(IFlowContext context)
        {
            _exitAction?.Invoke(context);
        }
    }
}