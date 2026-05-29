using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PowerCellStudio
{
    /// <summary>
    /// 驱动一组ISceneStep执行，有链式和并发两种模式
    /// </summary>
    public class FlowStepProcessor : IDisposable
    {
        private List<IFlowStep> _steps;

        internal FlowStepProcessor()
        {
            _steps = ListPool<IFlowStep>.Get();
        }

        public FlowState flowState { get; private set; } = FlowState.NotStarted;

        public int stepCount => _steps.Count;
        private int _currentStepIndex = 0;

        public void AddStep(IFlowStep step)
        {
            if (step == null) return;
            _steps.Add(step);
        }

        public void Reset()
        {
            flowState = FlowState.NotStarted;
            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i].Reset();
            }
            _currentStepIndex = 0;
        }

        public void ChainUpdate(IFlowContext context, float deltaTime)
        {
            if (context == null || flowState == FlowState.Completed || flowState == FlowState.Failed) return;

            if (flowState == FlowState.NotStarted)
            {
                Reset();
                flowState = FlowState.Running;
            }

            while (_currentStepIndex < _steps.Count)
            {
                var step = _steps[_currentStepIndex];
                if (step.state == FlowState.NotStarted)
                {
                    step.Start(context);
                }

                switch (step.state)
                {
                    case FlowState.NotStarted:
                        step.Start(context);
                        break;
                    case FlowState.Running:
                        step.Update(context, deltaTime);
                        return;
                    case FlowState.Completed:
                        step.Exit(context);
                        _currentStepIndex++;
                        break;
                    case FlowState.Failed:
                        flowState = FlowState.Failed;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i]?.OnSceneFlowed(context);
            }
            flowState = FlowState.Completed;
        }

        public void ParallelUpdate(IFlowContext context, float deltaTime)
        {
            if (context == null || flowState == FlowState.Completed || flowState == FlowState.Failed) return;
            if (flowState == FlowState.NotStarted)
            {
                Reset();
                for (var i = 0; i < _steps.Count; i++)
                {
                    _steps[i].Start(context);
                }

                flowState = FlowState.Running;
            }
            _currentStepIndex = _steps.Count;
            bool allDone = true;
            for (var i = 0; i < _steps.Count; i++)
            {
                var step = _steps[i];
                switch (step.state)
                {
                    case FlowState.NotStarted:
                        step.Start(context);
                        break;
                    case FlowState.Running:
                        step.Update(context, deltaTime);
                        allDone = false;
                        break;
                    case FlowState.Completed:
                        break;
                    case FlowState.Failed:
                        flowState = FlowState.Failed;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!allDone) return;
            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i].Exit(context);
            }
            // 严格在所有step.Exit调用后再OnSceneFlowed
            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i]?.OnSceneFlowed(context);
            }
            flowState = FlowState.Completed;
        }

        public void Dispose()
        {
            for (var i = 0; i < _steps.Count; i++)
            {
                _steps[i].Dispose();
            }

            _steps.Clear();
            ListPool<IFlowStep>.Release(_steps);
        }
    }
}