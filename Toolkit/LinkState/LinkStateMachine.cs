using System;
using System.Collections.Generic;
using PowerCellStudio;

namespace LinkState
{
    public class LinkStateMachine<T> where T : class, ILinkStateOwner, IDisposable
    {
        private T _owner;
        private bool _inExecution;
        private bool _inited;
        private Func<T, int> _initCondition;
        private bool _doExecute;

        public LinkStateMachine(T dataSource, bool doExecute, int size = 256)
        {
            if (dataSource == null)
            {
                LinkLog.LogError("StateMachine Got Null Source")
                return;
            }
            _inExecution = false;
            _inited = false;
            _doExecute = doExecute;
            _owner = dataSource;
            _statesTransition = new List<TriggerBehavior<T>>[size]();
            _statesExecute = new ExecuteBehavior<T>[size]();
        }
        
        private List<TriggerBehavior<T>>[]  _statesTransition;
        private ExecuteBehavior<T>[] _statesExecute;
        private int _currentStateIndex;

        public LinkStateMachine<T> SetEntry(Func<T, int> initConditionFunc)
        {
            _initCondition = initConditionFunc;
            _inited = false;
            return this;
        }

        public LinkStateMachine<T> SetExecute(int stateIndex, Action<T, float> executeAction)
        {
            if (_statesExecute.ContainsKey(stateIndex))
            {
                LinkLog.LogWarning("StateMachine has been set execute, Make sure you are not overwriting it");
            }
            _statesExecute[stateIndex] = new ExecuteBehavior<T>(executeAction);
            return this;
        }

        public LinkStateMachine<T> SetTrigger(int[] stateIndexes, Func<T, bool> trigger, Func<T, int> transition, TriggerPriority priority = TriggerPriority.Default)
        {
            foreach (var stateIndex in stateIndexes)
            {
                SetTrigger(stateIndex, trigger, transition, priority);
            }
            return this;
        }
        
        public LinkStateMachine<T> SetTrigger(int stateIndex, Func<T, bool> trigger, Func<T, int> transition, TriggerPriority priority = TriggerPriority.Default)
        {
            if (_statesTransition[stateIndex] == null)
                _statesTransition[stateIndex] = new List<TriggerBehavior<T>>();
            _statesTransition[stateIndex].Add(new TriggerBehavior<T>(trigger, transition, priority));
            return this;
        }
        
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

        public void Start()
        {
            if (_statesTransition == null) return;
            foreach (var triggers in _statesTransition)
            {
                triggers.Value.Sort((a,b)=>a.Priority.CompareTo(b.Priority));
            }
            _inExecution = true;
        }
        
        public void Stop() { _inExecution = false;}
        
        public void Restart() { _inited = false;}

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
                // trigger.Execute(_owner, deltaTime);
                if (!trigger.Check(_owner)) continue;
                _currentStateIndex = trigger.DoTransfer(_owner);
                _owner.StateIndex = _currentStateIndex;
                break;
            }
        }

        public void UpdateManually(int state, float dt)
        {
            if (!VerifyIndex(state)) return;
            _currentStateIndex = state;
            _owner.StateIndex = state;
            Update(dt);
        }

        private bool VerifyIndex(int index)
        {
            if(index < 0 || index > _statesTransition.Length - 1)
            {
                LinkLog.LogError($"index out of state range, got index = {index}, set range = [0, {_statesTransition.Length - 1}]")
                return false;
            }
            return true;
        }

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