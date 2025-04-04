using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    public class YieldInstructionsQueue
    {
            private Queue<YieldInstruction> _actions;
            private bool _pause;
            private float _interval;
            private Coroutine _coroutine;

            public YieldInstructionsQueue(float interval)
            {
                _interval = Mathf.Max(0f, interval);
                _actions = new Queue<YieldInstruction>();
            }

            public void SetInterval(float interval)
            {
                _interval = interval;
            }

            public void Push(YieldInstruction action)
            {
                _actions.Enqueue(action);
                if (_coroutine != null) return;
                _coroutine = ApplicationManager.instance.StartCoroutine(InvokeQueueYieldInstructions());
            }

            private IEnumerator InvokeQueueYieldInstructions()
            {
                if (_pause || _actions.Count == 0) yield break;
                while (_actions.Count > 0)
                {
                    if (_pause)
                    {
                        break;
                    }

                    var task = _actions.Dequeue();
                    if (task == null) continue;
                    yield return task;
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
                if (_coroutine != null) return;
                _pause = false;
                _coroutine = ApplicationManager.instance.StartCoroutine(InvokeQueueYieldInstructions());
            }

            public void Clear()
            {
                _actions.Clear();
            }
    }
}