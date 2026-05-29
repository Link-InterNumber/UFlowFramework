using System;

namespace PowerCellStudio
{
    public class SceneFlow : ISceneFlow
    {
        private readonly FlowStepProcessor _inSteps = new FlowStepProcessor();
        private readonly FlowStepProcessor _outSteps = new FlowStepProcessor();
        private readonly int _id;

        public SceneFlow(string flowName = null)
        {
            this._id = IndexGetter.instance.Get<ISceneFlow>();
            this.flowName = string.IsNullOrEmpty(flowName) ? GetType().Name : flowName;
        }

        public string flowName { get; }
        public int id => _id;

        public FlowState inState { get; protected set; } = FlowState.NotStarted;
        public FlowState outState { get; protected set; } = FlowState.NotStarted;

        public int stepCount => _inSteps.stepCount;
        
        public void AddStep(IFlowStep step)
        {
            if (step == null) return;
            _inSteps.AddStep(step);
        }

        public void AddTransitionStep(IFlowStep step)
        {
            if (step == null) return;
            _outSteps.AddStep(step);
        }

        public void Reset()
        {
            inState = FlowState.NotStarted;
            outState = FlowState.NotStarted;
            _inSteps.Reset();
            _outSteps.Reset();
        }

        public void StartFlow(IFlowContext context)
        {
            context.StartFlow(this);
            if (_inSteps.stepCount == 0)
            {
                inState = FlowState.Completed;
                context?.CompleteFlow();
                return;
            }
            if (inState == FlowState.NotStarted)
            {
                _inSteps.Reset();
                inState = FlowState.Running;
            }
        }

        public void UpdateFlow(IFlowContext context, float deltaTime)
        {
            if (context == null ||inState != FlowState.Running) return;
            _inSteps.ChainUpdate(context, deltaTime);
            switch (_inSteps.flowState)
            {
                case FlowState.NotStarted:
                    break;
                case FlowState.Running:
                    break;
                case FlowState.Completed:
                    if (!context.isFlowFailed)
                    {
                        context.CompleteFlow();
                    }
                    inState = FlowState.Completed;
                    break;
                case FlowState.Failed:
                    inState = FlowState.Failed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StartTransition(IFlowContext context)
        {
            if (_outSteps.stepCount == 0)
            {
                outState = FlowState.Completed;
                return;
            }
            if (outState == FlowState.NotStarted)
            {
                _outSteps.Reset();
                outState = FlowState.Running;
            }
        }

        public void UpdateTransition(IFlowContext context, float deltaTime)
        {
            if (context == null ||outState != FlowState.Running) return;
            _outSteps.ChainUpdate(context, deltaTime);
            switch (_outSteps.flowState)
            {
                case FlowState.NotStarted:
                    break;
                case FlowState.Running:
                    break;
                case FlowState.Completed:
                    outState = FlowState.Completed;
                    break;
                case FlowState.Failed:
                    outState = FlowState.Failed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual void OnExit(IFlowContext context)
        {
            
        }

        public void Dispose()
        {
            _inSteps.Dispose();
            _outSteps.Dispose();
        }
    }
}