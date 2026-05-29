using System;

namespace PowerCellStudio
{
    /// <summary>
    /// 驱动两个ISceneFlow转换
    /// </summary>
    public class TransitionProcessor : IDisposable
    {
        private ISceneFlow _fromFlow;
        private IFlowContext _fromContext;
        private IFlowRoadMap _fromRoadMap;
        private ISceneFlow _toFlow;
        private IFlowContext _toContext;
        private IFlowRoadMap _toRoadMap;

        public enum ProcessorMode
        {
            FlowTransition,
            PushRoadMap,
            PopRoadMap
        }
        private ProcessorMode _processorMode;
        
        private bool _fromFlowExited;
        public bool fromFlowExited => _fromFlowExited;
        
        private bool _completed;
        public bool completed => _completed;

        private bool _isRollingBack;
        public bool isRollingBack => _isRollingBack;

        public ISceneFlow fromFlow => _fromFlow;

        public IFlowRoadMap fromRoadMap => _fromRoadMap;

        public ISceneFlow toFlow => _toFlow;

        public IFlowRoadMap ToRoadMap => _toRoadMap;
        
        public ProcessorMode processorMode => _processorMode;

        public void Setup(ISceneFlow fromFlow, IFlowRoadMap fromRoadMap, ISceneFlow toFlow,
            IFlowRoadMap nextRoadMap, ProcessorMode mode)
        {
            _fromFlow = fromFlow;
            _fromContext = fromRoadMap?.context;
            _fromRoadMap = fromRoadMap;
            _toFlow = toFlow;
            _toContext = nextRoadMap?.context;
            _toRoadMap = nextRoadMap;
            _processorMode = mode;
            _fromFlowExited = false;
            _completed = false;
            _isRollingBack = false;
            _toFlow?.Reset();
        }

        public FlowState UpdateFromFlow(float deltaTime)
        {
            if (_fromFlow == null)
            {
                _fromFlowExited = true;
                return FlowState.Completed;
            }
            if (_completed || _fromFlowExited) return FlowState.Completed;
            if (_fromContext == null) return FlowState.Failed;
            
            if (_fromFlow.outState == FlowState.NotStarted)
            {
                _fromFlow.StartTransition(_fromContext);
            }

            switch (_fromFlow.outState)
            {
                case FlowState.NotStarted:
                    _fromFlow.StartTransition(_fromContext);
                    return FlowState.Running;
                case FlowState.Running:
                    _fromFlow.UpdateTransition(_fromContext, deltaTime);
                    return FlowState.Running;
                case FlowState.Completed:
                    _fromFlow.OnExit(_fromContext);
                    _fromFlowExited = true;
                    return FlowState.Completed;
                case FlowState.Failed:
                    _fromFlowExited = true;
                    return FlowState.Failed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public FlowState UpdateTargetFlow(float deltaTime)
        {
            if (_completed) return FlowState.Completed;
            if (_toFlow == null)
            {
                _completed = true;
                return FlowState.Completed;
            }
            if (_toFlow.inState == FlowState.NotStarted)
            {
                _toFlow.StartFlow(_toContext);
            }
            switch (_toFlow.inState)
            {
                case FlowState.NotStarted:
                    _toFlow.StartFlow(_toContext);
                    return FlowState.Running;
                case FlowState.Running:
                    _toFlow.UpdateFlow(_toContext, deltaTime);
                    return FlowState.Running;
                case FlowState.Completed:
                    _completed =  true;
                    return FlowState.Completed;
                case FlowState.Failed:
                    _completed = true;
                    return FlowState.Failed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PrepareRollback()
        {
            _fromFlow?.Reset();
            _isRollingBack = true;
        }
        
        public FlowState Rollback(float deltaTime)
        {
            if (_fromFlow == null) return FlowState.Failed;
            if (_fromFlow.inState == FlowState.NotStarted)
            {
                _fromFlow.StartFlow(_fromContext);
            }
            switch (_fromFlow.inState)
            {
                case FlowState.NotStarted:
                    _fromFlow.StartFlow(_fromContext);
                    return FlowState.Running;
                case FlowState.Running:
                    _fromFlow.UpdateFlow(_fromContext, deltaTime);
                    return FlowState.Running;
                case FlowState.Completed:
                    _isRollingBack = false;
                    return FlowState.Completed;
                case FlowState.Failed:
                    _isRollingBack = false;
                    return FlowState.Failed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Clear()
        {
            _fromFlow = null;
            _fromContext = null;
            _fromRoadMap = null;
            _toFlow = null;
            _toContext = null;
            _toRoadMap = null;
            _fromFlowExited = false;
            _completed = false;
            _isRollingBack = false;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}