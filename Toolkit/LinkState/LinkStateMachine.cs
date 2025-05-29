using System;
using System.Collections.Generic;
using PowerCellStudio;

namespace LinkState
{
    /// <summary>
    /// Represents a state machine for managing states and transitions.
    /// 表示一个用于管理状态和过渡的状态机。
    /// </summary>
    /// <typeparam name="T">The owner type which must implement ILinkStateOwner and IDisposable.
    /// 拥有者类型，必须实现 ILinkStateOwner 和 IDisposable。</typeparam>
    public class LinkStateMachine<T> where T : class, ILinkStateOwner, IDisposable
    {
        private T _owner;
        private bool _inExecution;
        private bool _inited;
        private Func<T, int> _initCondition;
        private bool _doExecute;

        /// <summary>
        /// Initializes a new instance of the LinkStateMachine.
        /// 初始化 LinkStateMachine 的新实例。
        /// </summary>
        /// <param name="dataSource">The source data for the state machine.
        /// 状态机的数据源。</param>
        /// <param name="doExecute">Whether to execute actions in states.
        /// 是否在状态中执行动作。</param>
        /// <param name="size">The size of the state machine arrays.
        /// 状态机数组的大小。</param>
        public LinkStateMachine(T dataSource, bool doExecute, int size = 256)
        {
            if (dataSource == null)
            {
                LinkLog.LogError("StateMachine Got Null Source");
                return;
            }
            _inExecution = false;
            _inited = false;
            _doExecute = doExecute;
            _owner = dataSource;
            _statesTransition = new List<TriggerBehavior<T>>[size];
            _statesExecute = new ExecuteBehavior<T>[size];
        }

        private List<TriggerBehavior<T>>[]  _statesTransition;
        private ExecuteBehavior<T>[] _statesExecute;
        private int _currentStateIndex;

        /// <summary>
        /// Sets the entry condition function.
        /// 设置入口条件函数。
        /// </summary>
        /// <param name="initConditionFunc">Function to determine the initial state.
        /// 用于确定初始状态的函数。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetEntry(Func<T, int> initConditionFunc)
        {
            _initCondition = initConditionFunc;
            _inited = false;
            return this;
        }

        /// <summary>
        /// Sets the execute action for a specific state.
        /// 为特定状态设置执行动作。
        /// </summary>
        /// <param name="stateIndex">The index of the state.
        /// 状态的索引。</param>
        /// <param name="executeAction">The action to execute for the state.
        /// 状态执行的动作。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetExecute(int stateIndex, Action<T, float> executeAction)
        {
            if (_statesExecute[stateIndex] != null)
            {
                LinkLog.LogWarning("StateMachine has been set execute, Make sure you are not overwriting it");
            }
            _statesExecute[stateIndex] = new ExecuteBehavior<T>(executeAction);
            return this;
        }

        /// <summary>
        /// Sets multiple triggers for state transitions.
        /// 设置多个状态转换触发器。
        /// </summary>
        /// <param name="stateIndexes">Array of state indexes.
        /// 状态索引数组。</param>
        /// <param name="trigger">Condition to trigger transition.
        /// 触发转换的条件。</param>
        /// <param name="transition">Function to determine the next state.
        /// 用于确定下一个状态的函数。</param>
        /// <param name="priority">Priority of the trigger.
        /// 触发器的优先级。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetTrigger(int[] stateIndexes, Func<T, bool> trigger, Func<T, int> transition, TriggerPriority priority = TriggerPriority.Default)
        {
            foreach (var stateIndex in stateIndexes)
            {
                SetTrigger(stateIndex, trigger, transition, priority);
            }
            return this;
        }

        /// <summary>
        /// Sets a trigger for a specific state transition.
        /// 为特定状态转换设置触发器。
        /// </summary>
        /// <param name="stateIndex">The index of the state.
        /// 状态的索引。</param>
        /// <param name="trigger">Condition to trigger transition.
        /// 触发转换的条件。</param>
        /// <param name="transition">Function to determine the next state.
        /// 用于确定下一个状态的函数。</param>
        /// <param name="priority">Priority of the trigger.
        /// 触发器的优先级。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetTrigger(int stateIndex, Func<T, bool> trigger, Func<T, int> transition, TriggerPriority priority = TriggerPriority.Default)
        {
            if (_statesTransition[stateIndex] == null)
                _statesTransition[stateIndex] = new List<TriggerBehavior<T>>();
            _statesTransition[stateIndex].Add(new TriggerBehavior<T>(trigger, transition, priority));
            return this;
        }

        /// <summary>
        /// Sets an escape trigger for a specific state.
        /// 为特定状态设置逃逸触发器。
        /// </summary>
        /// <param name="stateIndex">The index of the state.
        /// 状态的索引。</param>
        /// <param name="trigger">Condition to trigger escape.
        /// 触发逃逸的条件。</param>
        /// <param name="transition">Function to determine the next state.
        /// 用于确定下一个状态的函数。</param>
        /// <param name="priority">Priority of the trigger.
        /// 触发器的优先级。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetEscape(int stateIndex, Func<T, bool> trigger, Func<T, int> transition, TriggerPriority priority = TriggerPriority.Default)
        {
            if (_statesTransition[stateIndex] == null)
                _statesTransition[stateIndex] = new List<TriggerBehavior<T>>();
            _statesTransition[stateIndex].Add(new TriggerBehavior<T>((a) => {
                    if (!trigger(a)) return false;
                    _inExecution = false;
                    return true; 
                }, 
                transition, 
                priority));
            return this;
        }

        /// <summary>
        /// Starts the state machine execution.
        /// 开始状态机执行。
        /// </summary>
        public void Start()
        {
            if (_statesTransition == null) return;
            foreach (var triggers in _statesTransition)
            {
                triggers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
            _inExecution = true;
        }
        
        /// <summary>
        /// Stops the state machine execution.
        /// 停止状态机执行。
        /// </summary>
        public void Stop() { _inExecution = false; }
        
        /// <summary>
        /// Restarts the state machine execution.
        /// 重新启动状态机执行。
        /// </summary>
        public void Restart() { _inited = false; }

        /// <summary>
        /// Updates the state machine with a delta time.
        /// 使用增量时间更新状态机。
        /// </summary>
        /// <param name="deltaTime">The time difference for the update.
        /// 更新的时间差。</param>
        public void Update(float deltaTime)
        {
            if (!_inExecution) return;
            if (!_inited)
            {
                _currentStateIndex = _initCondition?.Invoke(_owner) ?? 0;
                _owner.StateIndex = _currentStateIndex;
                _inited = true;
            }
            if (!VerifyIndex(_currentStateIndex)) return;
            if (_doExecute && _statesExecute[_currentStateIndex] != null)
            {
                _statesExecute[_currentStateIndex].Execute(_owner, deltaTime);
            }

            var triggers = _statesTransition[_currentStateIndex];
            if (triggers == null) return;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (!trigger.Check(_owner)) continue;
                _currentStateIndex = trigger.DoTransfer(_owner);
                _owner.StateIndex = _currentStateIndex;
                break;
            }
        }

        /// <summary>
        /// Updates the state machine manually by specifying a state and delta time.
        /// 手动更新状态机，通过指定状态和增量时间。
        /// </summary>
        /// <param name="state">The state to update to.
        /// 要更新到的状态。</param>
        /// <param name="dt">The time difference for the update.
        /// 更新的时间差。</param>
        public void UpdateManually(int state, float dt)
        {
            if (!VerifyIndex(state)) return;
            _currentStateIndex = state;
            _owner.StateIndex = state;
            Update(dt);
        }

        /// <summary>
        /// Verifies the state index within the range.
        /// 验证状态索引是否在范围内。
        /// </summary>
        /// <param name="index">The index to verify.
        /// 要验证的索引。</param>
        /// <returns>True if the index is valid, otherwise false.
        /// 如果索引有效则为真，否则为假。</returns>
        private bool VerifyIndex(int index)
        {
            if (index < 0 || index > _statesTransition.Length - 1)
            {
                LinkLog.LogError(
                    $"index out of state range, got index = {index}, set range = [0, {_statesTransition.Length - 1}]");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Disposes the state machine, releasing resources.
        /// 处理状态机，释放资源。
        /// </summary>
        public void Dispose()
        {
            _inExecution = false;
            _inited = false;
            _owner = null;
            _statesTransition = null;
            _statesExecute = null;
            
        }
    }
}