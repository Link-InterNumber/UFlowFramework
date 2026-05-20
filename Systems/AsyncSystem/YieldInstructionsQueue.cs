using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class YieldInstructionsQueue : AsyncHandlerBase
    {
        private Queue<IEnumerator> _actions;
        private bool _pause;
        public bool paused => _pause;
        private float _interval;
        private Coroutine _coroutine;

        public override bool keepWaiting => _coroutine != null;

        private MonoBehaviour _monoBehaviour ;

        private int _completeCount;
        public int completeCount => _completeCount;

        private int _totalCount ;

        public float process => _totalCount == 0 ? 1f : (float)_completeCount / _totalCount;

        public YieldInstructionsQueue(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
            _interval = 0f;
            _actions = new Queue<IEnumerator>();
            _completeCount = 0;
            _totalCount = 0;
        }

        public YieldInstructionsQueue SetInterval(float interval)
        {
            _interval = Mathf.Max(0f, interval);
            return this;
        }

        public YieldInstructionsQueue Push(IEnumerator action)
        {
            _actions.Enqueue(action);
            _totalCount++;
            return this;
        }

        public void Start()
        {
            if (_actions.Count == 0) return;
            if (_coroutine != null) return;
            _coroutine = _monoBehaviour.StartCoroutine(InvokeQueueYieldInstructions());
        }

        private IEnumerator InvokeQueueYieldInstructions()
        {
            if (_pause || _actions.Count == 0) yield break;
            while (_actions.Count > 0)
            {
                if (_pause)
                {
                    yield return null;
                    continue;
                }

                var task = _actions.Dequeue();
                if (task == null) continue;
                yield return task;
                _completeCount++;
                if (_interval > 0f && _actions.Count > 0)
                    yield return new WaitForSecondsRealtime(_interval);
            }

            _coroutine = null;
            _onComplete?.Invoke();
            _onComplete = null;
        }

        public void Pause()
        {
            _pause = true;
        }

        public void Resume()
        {
            if (!_pause) return;
            _pause = false;
        }

        public void Clear()
        {
            _actions.Clear();
            _completeCount = 0;
            _onComplete = null;
        }

        public override void Cancel()
        {
            _actions.Clear();
            _pause = true;
            if (_coroutine != null)
            {
                _monoBehaviour.StopCoroutine(_coroutine);
                _coroutine = null;
            }
            _monoBehaviour = null;
            _onComplete = null;
        }
    }
}