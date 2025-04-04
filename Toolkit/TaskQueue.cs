using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xbox.Services.Client;
using UnityEngine;

namespace PowerCellStudio
{
    public class TaskQueue
    {
        private Queue<Task> _actions;
        private bool _pause;
        private float _interval;
        private bool _runInCoroutine;
        private Coroutine _coroutine;

        public TaskQueue(float interval, bool runInCoroutine)
        {
            _interval = Mathf.Max(0f, interval);
            _actions = new Queue<Task>();
            _runInCoroutine = runInCoroutine;
        }

        public void SetInterval(float interval)
        {
            _interval = interval;
        }

        public void Push(Task action)
        {
            _actions.Enqueue(action);
            if (!_runInCoroutine) InvokeQueue();
            else
            {
                if(_coroutine != null) return;
                _coroutine = ApplicationManager.instance.StartCoroutine(InvokeQueueYieldInstructions());
            }
        }

        private async void InvokeQueue()
        {
            if(_pause || _actions.Count == 0) return;
            var task = _actions.Dequeue();
            if (task != null)
            {
                await task;
                await Task.Delay(Mathf.CeilToInt(_interval * 1000));
            }
            InvokeQueue();
        }

        private IEnumerator InvokeQueueYieldInstructions()
        {
            if(_pause || _actions.Count == 0) yield break;
            while (_actions.Count > 0)
            {
                if(_pause)
                {
                    break;
                }
                var task = _actions.Dequeue();
                if(task == null) continue;
                yield return task.AsCoroutine();
                yield return new WaitForSecondsRealtime(_interval);
            }
            _coroutine = null;
        }

        public void Pause()
        {
            _pause = true;
        }

        public void Resume()
        {
            _pause = false;
            if (!_runInCoroutine) InvokeQueue();
            else
            {
                _coroutine = ApplicationManager.instance.StartCoroutine(InvokeQueueYieldInstructions());
            }
        }

        public void Clear()
        {
            _actions.Clear();
        }
    }
}