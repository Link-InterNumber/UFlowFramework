using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public sealed class IntervalCallRunner : IDisposable
    {
        private class IntervalTask: PoolObject, IIndex
        {
            public float interval;
            public float elapsedTime;
            public System.Action action;
            public Func<bool> autoReleaseCondition;

            private int _index;
            public int index => _index;

            public void Setup(float taskInterval, System.Action taskAction, Func<bool> autoRelease)
            {
                _index = IndexGetter.instance.Get<IntervalTask>();
                interval = taskInterval;
                elapsedTime = 0f;
                action = taskAction;
                autoReleaseCondition = autoRelease;
            }

            public override void OnDeSpawn()
            {
                interval = 0f;
                elapsedTime = 0f;
                action = null;
                autoReleaseCondition = null;
            }

            public override void OnSpawn(){}

            public override void Dispose()
            {
                base.Dispose();
                action = null;
                autoReleaseCondition = null;
            }
        }

        private PoolableObjectPool<IntervalTask> _taskPool = new PoolableObjectPool<IntervalTask>(() => new IntervalTask(), 10, 5);
        private Dictionary<int, IntervalTask> _taskMap = new Dictionary<int, IntervalTask>();
        private HashSet<int> _tasksToRemove = new HashSet<int>();

        public void Dispose()
        {
            foreach (var task in _taskMap.Values)
            {
                task.Dispose();
            }
            _taskMap.Clear();
            _taskPool.Dispose();
            _tasksToRemove.Clear();
            _taskMap = null;
            _taskPool = null;
            _tasksToRemove = null;
        }

        public int RegisterTask(float interval, System.Action action, Func<bool> autoReleaseCondition)
        {
            if (interval <= 0 || action == null) return -1;
            var task = _taskPool.Get();
            task.Setup(interval, action, autoReleaseCondition);
            _taskMap[task.index] = task;
            return task.index;
        }

        public void UnregisterTask(int taskIndex)
        {
            if (!_taskMap.ContainsKey(taskIndex)) return;
            _tasksToRemove.Add(taskIndex);
        }

        public void Tick(float deltaTime)
        {
            if (_tasksToRemove.Count > 0)
            {
                foreach (var taskIndex in _tasksToRemove)
                {
                    if (_taskMap.TryGetValue(taskIndex, out var task))
                    {
                        _taskMap.Remove(taskIndex);
                        task.DeSpawn();
                    }
                }
                _tasksToRemove.Clear();
            }

            if (_taskMap.Count == 0) return;
            foreach (var task in _taskMap.Values)
            {
                task.elapsedTime += deltaTime;
                if (task.elapsedTime >= task.interval)
                {
                    task.action?.Invoke();
                    task.elapsedTime = 0f;
                }
                if (task.autoReleaseCondition != null && task.autoReleaseCondition())
                {
                    _tasksToRemove.Add(task.index);
                }
            }
        }

        public void ClearAllTasks()
        {
            foreach (var task in _taskMap.Values)
            {
                _tasksToRemove.Add(task.index);
            }
        }
    }
}