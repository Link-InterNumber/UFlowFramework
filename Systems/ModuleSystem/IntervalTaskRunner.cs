namespace PowerCellStudio
{
    public class IntervalTaskRunner : MonoSingleton<IntervalTaskRunner>
    {
        private class IntervalTask
        {
            public float interval;
            public float elapsedTime;
            public System.Action action;
        }
        
        private readonly System.Collections.Generic.List<IntervalTask> _tasks = new();
        private readonly System.Collections.Generic.List<System.Action> _runBuffer = new();
        private readonly System.Collections.Generic.List<System.Action> _toRemove = new();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _tasks.Clear();
            _runBuffer.Clear();
            _toRemove.Clear();
        }

        public void RegisterTask(float interval, System.Action action)
        {
            if (interval <= 0 || action == null) return;
            _tasks.Add(new IntervalTask()
            {
                interval = interval,
                elapsedTime = 0f,
                action = action
            });
        }
        
        public void UnregisterTask(System.Action action)
        {
            _toRemove.Add(action);
        }
        
        private void FixedUpdate()
        {
            float deltaTime = UnityEngine.Time.fixedDeltaTime;
            // Remove tasks marked for removal
            if (_toRemove.Count > 0)
            {
                foreach (var action in _toRemove)
                {
                    _tasks.RemoveAll(t => t.action == action);
                }
                _toRemove.Clear();
            }
            if (_tasks.Count == 0) return;
            // Update tasks and collect actions to run
            foreach (var task in _tasks)
            {
                task.elapsedTime += deltaTime;
                if (task.elapsedTime >= task.interval)
                {
                    _runBuffer.Add(task.action);
                    task.elapsedTime = 0f;
                }
            }
            // Execute collected actions
            if (_runBuffer.Count == 0) return;
            foreach (var action in _runBuffer)
            {
                action?.Invoke();
            }
            _runBuffer.Clear();
        }
    }
}