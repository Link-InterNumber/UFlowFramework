using System;
using System.Collections.Generic;
using PowerCellStudio;

namespace LinkState
{
    /// <summary>
    /// Represents a state machine for managing states and transitions.
    /// 表示一个用于管理状态和过渡的状态机。
    /// </summary>
    /// <typeparam name="T">The owner type which must implement ILinkStateOwner 
    /// 拥有者类型，必须实现 ILinkStateOwner 和 IDisposable。</typeparam>
    public class LinkStateMachine<T> : IDisposable where T : class, ILinkStateOwner
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
                LinkLogger.LogError("StateMachine Got Null Source");
                return;
            }
            _inExecution = false;
            _inited = false;
            _doExecute = doExecute;
            _owner = dataSource;
            _statesTransition = new List<TriggerBehavior<T>>[size];
            _statesExecute = new ExecuteBehavior<T>[size];
            _statesEnter = new Action<T>[size];
            _statesExit = new Action<T>[size];
        }

        private List<TriggerBehavior<T>>[]  _statesTransition;
        private ExecuteBehavior<T>[] _statesExecute;
        private Action<T>[] _statesEnter;
        private Action<T>[] _statesExit;
        private int _currentStateIndex;

        public bool executeOnUpdate
        {
            get => _doExecute;
            set => _doExecute = value;
        }

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

        private bool ChechIndex(int index)
        {
            if (index < 0 || index > _statesTransition.Length - 1)
            {
                LinkLogger.LogError($"index out of state range, got index = {index}, set range = [0, {_statesTransition.Length - 1}]");
                return false;
            }
            return true;
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
            if (!ChechIndex(stateIndex)) return this;
            if (_statesExecute[stateIndex] != null)
            {
                LinkLogger.LogWarning("StateMachine has been set execute, Make sure you are not overwriting it");
            }
            _statesExecute[stateIndex] = new ExecuteBehavior<T>(executeAction);
            return this;
        }

        /// <summary>
        /// Sets the enter action for a specific state.
        /// 为特定状态设置进入动作。
        /// </summary>
        /// <param name="stateIndex">The index of the state.
        /// 状态的索引。</param>
        /// <param name="enterAction">The action to execute when entering the state.
        /// 进入状态时执行的动作。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetEnter(int stateIndex, Action<T> enterAction)
        {
            if (!ChechIndex(stateIndex)) return this;
            if (_statesEnter[stateIndex] != null)
            {
                LinkLogger.LogWarning("StateMachine has been set enter, Make sure you are not overwriting it");
            }
            _statesEnter[stateIndex] = enterAction;
            return this;
        }

        /// <summary>
        /// Sets the exit action for a specific state.
        /// 为特定状态设置退出动作。
        /// </summary>
        /// <param name="stateIndex">The index of the state.
        /// 状态的索引。</param>
        /// <param name="exitAction">The action to execute when exiting the state.
        /// 退出状态时执行的动作。</param>
        /// <returns>The current state machine instance.
        /// 当前状态机实例。</returns>
        public LinkStateMachine<T> SetExit(int stateIndex, Action<T> exitAction)
        {
            if (!ChechIndex(stateIndex)) return this;
            if (_statesExit[stateIndex] != null)
            {
                LinkLogger.LogWarning("StateMachine has been set exit, Make sure you are not overwriting it");
            }
            _statesExit[stateIndex] = exitAction;
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
            if (!ChechIndex(stateIndex)) return this;
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
            if (!ChechIndex(stateIndex)) return this;
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
                if (triggers == null) continue;
                triggers.Sort((a, b) =>
                {
                    if (a == null) return -1;
                    if (b == null) return 1;
                    if (a.Priority == b.Priority) return 0;
                    return a.Priority > b.Priority ? -1 : 1;
                } );
            }
            _inExecution = true;
        }
        
        /// <summary>
        /// Stops the state machine execution.
        /// 停止状态机执行。
        /// </summary>
        public void Stop()
        {
            if (_inExecution && _inited)
            {
                InvokeExit(_currentStateIndex);
            }
            _inExecution = false;
        }
        
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
                ChangeState(_initCondition?.Invoke(_owner) ?? 0, false);
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
                ChangeState(trigger.DoTransfer(_owner), true);
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
            if (!_inited)
            {
                _inited = true;
            }
            ChangeState(state, _inExecution || _inited);
            Update(dt);
        }

        private void ChangeState(int nextStateIndex, bool invokeExit)
        {
            if (!VerifyIndex(nextStateIndex)) return;

            var previousStateIndex = _currentStateIndex;
            var hasPreviousState = _inited && VerifyIndex(previousStateIndex);
            if (hasPreviousState && invokeExit && previousStateIndex != nextStateIndex)
            {
                InvokeExit(previousStateIndex);
            }

            _currentStateIndex = nextStateIndex;
            _owner.StateIndex = _currentStateIndex;

            if (!hasPreviousState || previousStateIndex != nextStateIndex)
            {
                InvokeEnter(_currentStateIndex);
            }
        }

        private void InvokeEnter(int stateIndex)
        {
            _statesEnter[stateIndex]?.Invoke(_owner);
        }

        private void InvokeExit(int stateIndex)
        {
            _statesExit[stateIndex]?.Invoke(_owner);
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
                LinkLogger.LogError(
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
            if (_inExecution && _inited && VerifyIndex(_currentStateIndex))
            {
                InvokeExit(_currentStateIndex);
            }
            _inExecution = false;
            _inited = false;
            _owner = null;
            _statesTransition = null;
            _statesExecute = null;
            _statesEnter = null;
            _statesExit = null;
            
        }
    }
}