using System;

namespace PowerCellStudio
{
    public class WaitUntilFlowStep : FlowStepBase
    {
        private readonly Func<IFlowContext, bool> _predicate;
        private readonly float _timeout;
        private readonly Action<IFlowContext> _onTimeout;
        private float _elapsed;

        public WaitUntilFlowStep(Func<IFlowContext, bool> predicate, string stepName = null, float timeout = -1f,
            Action<IFlowContext> onTimeout = null) : base(stepName)
        {
            _predicate = predicate;
            _timeout = timeout;
            _onTimeout = onTimeout;
        }

        protected override void OnReset()
        {
            _elapsed = 0f;
        }

        protected override void OnStart(IFlowContext context)
        {
            if (_predicate?.Invoke(context) ?? false)
            {
                CompleteStep();
            }
        }

        protected override void OnUpdate(IFlowContext context, float deltaTime)
        {
            if (_predicate?.Invoke(context) ?? false)
            {
                CompleteStep();
                return;
            }

            if (_timeout < 0f) return;

            _elapsed += deltaTime;
            if (_elapsed < _timeout) return;

            _onTimeout?.Invoke(context);
            Fail(context);
        }

        protected override void OnFail(IFlowContext context)
        {
            base.OnFail(context);
            context?.FailFlow($"Flow step timeout: {stepName}");
        }
    }
}