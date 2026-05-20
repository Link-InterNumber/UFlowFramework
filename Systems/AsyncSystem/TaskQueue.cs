using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PowerCellStudio
{
    public class TaskQueue : AsyncHandlerBase
    {
        private Queue<Task> _actions;
        private bool _pause;
        private float _interval;

        public override bool keepWaiting => _actions?.Count > 0;

        public TaskQueue(float interval)
        {
            _interval = Mathf.Max(0f, interval);
            _actions = new Queue<Task>();
        }

        public TaskQueue SetInterval(float interval)
        {
            _interval = interval;
            return this;
        }

        public TaskQueue Push(Task action)
        {
            _actions.Enqueue(action);
            return this;
        }

        public void Statrt()
        {
            if (_actions.Count == 0) return;
            InvokeQueue();
        }

        private async void InvokeQueue()
        {
            if(_pause) return;
            if (_actions.Count == 0)
            {
                _onComplete?.Invoke();
                return;
            }
            var task = _actions.Peek();
            if (task != null)
            {
                if (task.IsCompleted)
                {
                    // 任务已经完成，直接继续下一个
                    _actions.Dequeue();
                    InvokeQueue();
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError($"Task failed: {task.Exception}");
                    _actions.Dequeue();
                    InvokeQueue();
                    return;
                }
                if (task.IsCanceled)
                {
                    Debug.LogWarning("Task was canceled.");
                    _actions.Dequeue();
                    InvokeQueue();
                    return;
                }
                await task;
                await Task.Delay(Mathf.CeilToInt(_interval * 1000));
            }
            _actions.Dequeue();
            InvokeQueue();
        }

        public void Pause()
        {
            _pause = true;
        }

        public void Resume()
        {
            if (!_pause) return;
            _pause = false;
            InvokeQueue();
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public override void Cancel()
        {
            _pause = true;
            _actions.Clear();
        }
    }
}