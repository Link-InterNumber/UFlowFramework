using System.Collections.Generic;

namespace PowerCellStudio
{
    public class FlowContext : IFlowContext
    {
        private Dictionary<string, object> _values;
        private Dictionary<string, bool> _trigger;
        private static Dictionary<string, object> _sharedValues = new Dictionary<string, object>();
        public static void ClearAllSharedValues()
        {
            _sharedValues.Clear();
        }

        public FlowContext(object data = null)
        {
            contextData = data;
            _values = new Dictionary<string, object>();
            _trigger = new Dictionary<string, bool>();
        }

        public object contextData { get; set; }
        
        public ISceneFlow currentFlow { get; private set; }

        public ISceneFlow previousFlow { get; private set; }

        public bool isFlowCompleted { get; private set; }

        public bool isFlowFailed { get; private set; }

        public string failureReason { get; private set; }

        public void StartFlow(ISceneFlow nextFlow)
        {
            MoveToFlow(nextFlow);
        }

        public void CompleteFlow()
        {
            if (isFlowFailed) return;
            isFlowCompleted = true;
        }

        public void FailFlow(string reason = null)
        {
            if (isFlowCompleted) return;
            isFlowFailed = true;
            failureReason = reason;
        }

        public void SetValue(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _values[key] = value;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            if (_values == null || string.IsNullOrEmpty(key)) return false;
            if (!_values.TryGetValue(key, out var storedValue)) return false;
            if (storedValue is not T typedValue) return false;
            value = typedValue;
            return true;
        }
        
        public void SetTrigger(string key, bool isOn)
        {
            _trigger[key] = isOn;
        }
        
        public bool IsTriggerOn(string key)
        {
            return _trigger.TryGetValue(key, out var isOn) ? isOn : false;
        }
        
        public bool CheckTrigger(string key)
        {
            if (_trigger.TryGetValue(key, out var isOn) &&  isOn)
            {
                _trigger.Remove(key);
                return true;
            }
            return false;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            return TryGetValue<T>(key, out var value) ? value : defaultValue;
        }

        public bool ClearValue(string key)
        {
            return _values.Remove(key);
        }

        public void ClearAllValues()
        {
            _values.Clear();
        }

        public void SetSharedValue(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _sharedValues[key] = value;
        }

        public bool TryGetSharedValue<T>(string key, out T value)
        {
            value = default;
            if (_sharedValues == null || string.IsNullOrEmpty(key)) return false;
            if (!_sharedValues.TryGetValue(key, out var storedValue)) return false;
            if (storedValue is not T typedValue) return false;
            value = typedValue;
            return true;
        }

        public T GetSharedValue<T>(string key, T defaultValue = default)
        {
            return TryGetSharedValue<T>(key, out var value) ? value : defaultValue;
        }

        public bool ClearSharedValue(string key)
        {
            return _sharedValues.Remove(key);
        }

        internal Dictionary<string, object> values => _values;

        Dictionary<string, object> IFlowContext.sharedValues => _sharedValues;

        internal void MoveToFlow(ISceneFlow nextFlow)
        {
            if (currentFlow != null)
            {
                previousFlow = currentFlow;
            }

            currentFlow = nextFlow;
            ResetFlowState();
        }

        internal void ResetFlowState()
        {
            isFlowCompleted = false;
            isFlowFailed = false;
            failureReason = null;
        }

        public void Dispose()
        {
            _values.Clear();
            _trigger.Clear();
            currentFlow = null;
            previousFlow = null;
            contextData = null;
            failureReason = null;
            _values = null;
            _trigger = null;
            isFlowCompleted = false;
            isFlowFailed = false;
        }
    }
}