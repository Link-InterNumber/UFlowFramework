using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    /// <summary>
    /// 负责管理IRoadMap出/入栈、驱动TransitionProcessor、手动/在修改黑板值时检查是否能进行ISceneFlow转换
    /// </summary>
    public class FlowManager : SingletonBase<FlowManager>, IExecutionModule, IOnGameResetModule
    {
        private const int TransitionProcessorPoolMaxSize = 8;
        private const int TransitionProcessorPoolInitSize = 2;

        private struct RoadMapRuntimeFrame
        {
            public IFlowRoadMap roadMap;
            public IFlowContext context;
            public ISceneFlow returnFlow;
            public int returnTransitionId;
        }

        private IFlowRoadMap _currentRoadMap;
        private LinkedList<RoadMapRuntimeFrame> _roadMapFrames = new LinkedList<RoadMapRuntimeFrame>();
        private LinkPool<TransitionProcessor> _transitionProcessorPool;
        
        private Queue<TransitionProcessor> _tpQueue = new Queue<TransitionProcessor>();
        private TransitionProcessor _currentTP => _tpQueue.Peek();

        public IFlowRoadMap currentRoadMap => _currentRoadMap;
        public IFlowContext currentContext => _currentRoadMap?.context;
        public ISceneFlow currentFlow => currentContext?.currentFlow;
        public ISceneFlow previousFlow => currentContext?.previousFlow;

        public bool inTransition => _tpQueue.Count > 0;

        public void OnInit()
        {
            _transitionProcessorPool = new LinkPool<TransitionProcessor>(() => new TransitionProcessor(),
                TransitionProcessorPoolInitSize, TransitionProcessorPoolMaxSize);
        }

        public void OnGameReset()
        {
            FlowContext.ClearAllSharedValues();
            var disposedRoadMaps = new HashSet<IFlowRoadMap>();
            foreach (var frame in _roadMapFrames)
            {
                if (frame.roadMap != null)
                {
                    disposedRoadMaps.Add(frame.roadMap);
                }
            }

            foreach (var roadMap in disposedRoadMaps)
            {
                roadMap.Dispose();
            }

            while (_tpQueue.Count > 0)
            {
                var tp = _tpQueue.Dequeue();
                ReleaseTransitionProcessor(tp);
            }

            _roadMapFrames.Clear();
            _currentRoadMap = null;
        }
        
        public bool inExecution { get; set; }
        public void Execute(float dt)
        {
            if (inTransition)
            {
                var currentTP = _currentTP;
                if (!currentTP.fromFlowExited)
                {
                    if (currentTP.fromFlow == null)
                    {
                        currentTP.Setup(currentContext.currentFlow, _currentRoadMap, currentTP.toFlow, currentTP.ToRoadMap, currentTP.processorMode);
                    }
                    if (currentTP.fromFlow != currentContext.currentFlow)
                    {
                        // 场景流不匹配则直接结束TransitionProcessor
                        ReleaseTransitionProcessor(_tpQueue.Dequeue());
                        return;
                    }
                    var state = currentTP.UpdateFromFlow(dt);
                    switch (state)
                    {
                        case FlowState.NotStarted:
                            return;
                        case FlowState.Running:
                            return;
                        case FlowState.Completed:
                            if (currentTP.processorMode == TransitionProcessor.ProcessorMode.PushRoadMap && currentTP.ToRoadMap != null)
                                PushRoadMapToStack(currentTP.ToRoadMap);
                            else if (currentTP.processorMode == TransitionProcessor.ProcessorMode.PopRoadMap)
                                PopRoadMap();
                            break;
                        case FlowState.Failed:
                            // 失败则留在原来的sceneFlow
                            ReleaseTransitionProcessor(_tpQueue.Dequeue());
                            return;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (!currentTP.completed)
                {
                    var state = currentTP.UpdateTargetFlow(dt);
                    switch (state)
                    {
                        case FlowState.NotStarted:
                            return;
                        case FlowState.Running:
                            return;
                        case FlowState.Completed:
                            ReleaseTransitionProcessor(_tpQueue.Dequeue());
                            TryTriggerTransition();
                            return;
                        case FlowState.Failed:
                            // 失败则回退到前一个sceneFlow
                            if (currentTP.processorMode == TransitionProcessor.ProcessorMode.PushRoadMap && currentTP.ToRoadMap != null)
                            {
                                PopRoadMap();
                            }
                            else if (currentTP.processorMode == TransitionProcessor.ProcessorMode.PopRoadMap)
                            {
                                PushRoadMapToStack(currentTP.fromRoadMap);
                            }
                            currentTP.PrepareRollback();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (_currentRoadMap == null)
                {
                    var tp = _tpQueue.Dequeue();
                    ReleaseTransitionProcessor(tp);
                    return;
                }

                if (currentTP.isRollingBack)
                {
                    var state = currentTP.Rollback(dt);
                    switch (state)
                    {
                        case FlowState.NotStarted:
                            break;
                        case FlowState.Running:
                            break;
                        case FlowState.Completed:
                            ReleaseTransitionProcessor(_tpQueue.Dequeue());
                            return;
                        case FlowState.Failed:
                            // 回退失败则回到roadMap入口
                            var fromRoadMap = currentTP.fromRoadMap;
                            var fromSceneFlow = currentTP.fromFlow;
                            ReleaseTransitionProcessor(_tpQueue.Dequeue());
                            if (fromRoadMap != null && fromRoadMap.entryFlow != fromSceneFlow)
                            {
                                // 回退到roadMap入口
                                var newTp = GetTransitionProcessor();
                                newTp.Setup(null, null, fromRoadMap.entryFlow, fromRoadMap, TransitionProcessor.ProcessorMode.FlowTransition);
                                _tpQueue.Enqueue(newTp);
                            }
                            else
                            {
                                // 回退到前一个roadMap
                                ExitCurrentRoadMap();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                return;
            }
        }

        private void PushRoadMapToStack(IFlowRoadMap roadMap)
        {
            _roadMapFrames.AddLast(new RoadMapRuntimeFrame
            {
                roadMap = roadMap,
                context = roadMap.context,
            });
            _currentRoadMap = roadMap;
        }

        private void PopRoadMap()
        {
            if (_roadMapFrames.Count > 0) _roadMapFrames.RemoveLast();
            _currentRoadMap = _roadMapFrames.Count > 0 ? _roadMapFrames.Last.Value.roadMap : null;
        }

        private TransitionProcessor GetTransitionProcessor()
        {
            return _transitionProcessorPool.Get();
        }

        private void ReleaseTransitionProcessor(TransitionProcessor processor)
        {
            processor.Clear();
            if (!_transitionProcessorPool.Release(processor))
            {
                processor.Dispose();
            }
        }

        public void EnterRoadMap(IFlowRoadMap roadMap)
        {
            if (roadMap == null) return;
            if (roadMap.entryFlow == null)
            {
                ModuleLogger.LogError<FlowManager>("roadMap has no entryFlow!");
                return;
            }

            if (roadMap.context == null)
            {
                ModuleLogger.LogError<FlowManager>("roadMap has no context!");
                return;
            }
            var processor = GetTransitionProcessor();
            processor.Setup(null, null, roadMap.entryFlow, roadMap, TransitionProcessor.ProcessorMode.PushRoadMap);
            _tpQueue.Enqueue(processor);
        }

        public void ExitCurrentRoadMap()
        {
            if (_roadMapFrames.Count == 0) return;
            var processor = GetTransitionProcessor();
            var previous = _roadMapFrames.Count > 1 ? _roadMapFrames.Last.Previous.Value.roadMap : null;
            processor.Setup(null, null, previous?.context.currentFlow, previous, TransitionProcessor.ProcessorMode.PopRoadMap);
            _tpQueue.Enqueue(processor);
        }

        public void SetContextValue(string key, object value, bool shared)
        {
            if (currentContext == null) return;
            if (shared) currentContext.SetSharedValue(key, value);
            else currentContext.SetValue(key, value);
            TryTriggerTransition();
        }

        public void SetContextTrigger(string key, bool value)
        {
            if (currentContext == null) return;
            currentContext.SetTrigger(key, value);
            TryTriggerTransition();
        }

        public void TryTriggerTransition()
        {
            if (TryGetTransition(out var transition))
                _tpQueue.Enqueue(transition);
        }

        private bool TryGetTransition(out TransitionProcessor processor)
        {
            processor = null;
            if (_currentRoadMap == null)
                return false;
            if (inTransition)
                return false;

            var found = _currentRoadMap.TryGetTransition(_currentRoadMap.context.currentFlow, out var toFlow);
            if (found)
            {
                processor = GetTransitionProcessor();
                processor.Setup(_currentRoadMap.context.currentFlow, _currentRoadMap, toFlow, null, TransitionProcessor.ProcessorMode.FlowTransition);
                return true;
            }
            return false;
        }

        public void ForceTransition(int targetSceneFlowId)
        {
            if (_currentRoadMap == null) return;
            if (inTransition) return;

            var found = _currentRoadMap.GetSceneFlow(targetSceneFlowId);
            if (found != null)
            {
                var processor = GetTransitionProcessor();
                processor.Setup(null, null, found, _currentRoadMap, TransitionProcessor.ProcessorMode.FlowTransition);
                _tpQueue.Enqueue(processor);
            }
        }
    }
}