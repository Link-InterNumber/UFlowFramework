using System;
using System.Collections.Generic;
using PowerCellStudio;

namespace LinkState
{
    public class LinkStateMachine<T> where T : class, ILinkStateOwner
    {
        private T _owner;
        private bool _inExecution;
        private bool _inited;
        private Func<T, int> _initCondition;
        private bool _doExecute;

        public LinkStateMachine(T dataSource, bool doExecute)
        {
            _inExecution = false;
            _inited = false;
            _doExecute = doExecute;
            _owner = dataSource ?? throw new Exception("StateMachine Got Null Source");
            _statesTransition = new Dictionary<int, List<TriggerBehavior<T>>>();
            _statesExecute = new Dictionary<int, ExecuteBehavior<T>>();
        }
        
        private Dictionary<int, List<TriggerBehavior<T>>>  _statesTransition;
        private Dictionary<int, ExecuteBehavior<T>> _statesExecute;
        private int _currentStateIndex;

        public LinkStateMachine<T> SetEntry(Func<T, int> initConditionFunc)
        {
            _initCondition = initConditionFunc;
            _inited = false;
            return this;
        }

        public LinkStateMachine<T> SetExecute(int stateIndex, Action<T, float> executeAction)
        {
            if(_statesExecute.ContainsKey(stateIndex))
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
            if (!_statesTransition.ContainsKey(stateIndex))
                _statesTransition[stateIndex] = new List<TriggerBehavior<T>>();
            _statesTransition[stateIndex].Add(new TriggerBehavior<T>(trigger, transition, priority));
            return this;
        }
        
        public LinkStateMachine<T> SetEscape(int stateIndex, Func<T, bool> trigger, Func<T, int> transition, TriggerPriority priority = TriggerPriority.Default)
        {
            if (!_statesTransition.ContainsKey(stateIndex))
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
            if(_statesTransition == null) return;
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

            if (_doExecute && _statesExecute.TryGetValue(_currentStateIndex, out var executeAction))
            {
                executeAction.Execute(_owner, deltaTime);
            }

            _statesTransition.TryGetValue(_currentStateIndex, out var triggers);
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
            _currentStateIndex = state;
            _owner.StateIndex = state;
            Update(dt);
        }
    }
}