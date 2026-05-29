namespace PowerCellStudio
{
    public class DelayFlowStep : FlowStepBase
    {
        private readonly float _duration;
        private float _elapsed;

        public DelayFlowStep(float duration, string stepName = null) : base(stepName)
        {
            _duration = duration < 0f ? 0f : duration;
        }

        protected override void OnReset()
        {
            _elapsed = 0f;
        }

        protected override void OnStart(IFlowContext context)
        {
            if (_duration <= 0f)
            {
                CompleteStep();
            }
        }

        protected override void OnUpdate(IFlowContext context, float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed >= _duration)
            {
                CompleteStep();
            }
        }
    }
}