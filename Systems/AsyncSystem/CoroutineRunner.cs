using System;
using System.Collections;
using UnityEngine;

namespace PowerCellStudio
{
    [System.CLSCompliant(false)]
    public sealed class CoroutineRunner : IDisposable
    {
        private readonly MonoBehaviour _host;
        private PoolableObjectPool<CoroutineHandler> _coroutineHandlerPool;

        public CoroutineRunner(MonoBehaviour host)
        {
            _host = host;
            _coroutineHandlerPool = new PoolableObjectPool<CoroutineHandler>(() => new CoroutineHandler(), 10, 5);
        }

        public void Dispose()
        {
            _host?.StopAllCoroutines();
            _coroutineHandlerPool?.Dispose();
            _coroutineHandlerPool = null;
        }

        #region Coroutine
        private class CoroutineWrapper: IEnumerator
        {
            private IEnumerator _routine;
            private bool _hasError;

            public CoroutineWrapper(IEnumerator routine)
            {
                _routine = routine;
                _hasError = false;
            }
            
            public bool MoveNext()
            {
                if (_hasError)
                    return false;

                try
                {
                    return _routine.MoveNext();
                }
                catch (Exception ex)
                {
                    _hasError = true;
                    Debug.LogError($"Coroutine error: {ex.Message}\n{ex.StackTrace}");
                    // 可以在这里添加更多的错误处理逻辑
                    return false;
                }
            }

            public void Reset()
            {
                _hasError = false;
                _routine.Reset();
            }

            public object Current => _routine.Current;
        }

        private IEnumerator LogableCoroutine(IEnumerator routine)
        {
            var wrapper = new CoroutineWrapper(routine);
            while (wrapper.MoveNext())
            {
                yield return wrapper.Current;
            }
        }

        public CoroutineHandler RunCoroutine(IEnumerator routine)
        {
            var handler = _coroutineHandlerPool.Get();
#if UNITY_EDITOR
            handler.Assembly(_host, LogableCoroutine(routine));
            return handler;
#else
            handler.Assembly(_host, routine);
            return handler;
#endif
        }

        public CoroutineHandler StartCoroutine(IEnumerator routine)
        {
            return RunCoroutine(routine);
        }

        // public CoroutineHandler DelayedFrame(Action call, int frameCount = 1)
        // {
        //     return RunCoroutine(DelayedFrameHandler(call, frameCount));
        // }
        
        // private static IEnumerator DelayedFrameHandler(Action call, int frameCount)
        // {
        //     for (int i = 0; i < frameCount; i++)
        //     {
        //         yield return null;
        //     }
        //     call?.Invoke();
        // }

        // public CoroutineHandler DelayedCall(float timeInSecond, Action call, bool ignoreTimeScale = true)
        // {
        //     return RunCoroutine(DelayedCallHandler(timeInSecond, call, ignoreTimeScale));
        // }

        // private static IEnumerator DelayedCallHandler(float timeInSecond, Action call, bool ignoreTimeScale)
        // {
        //     if (ignoreTimeScale) yield return new WaitForSecondsRealtime(timeInSecond);
        //     else yield return new WaitForSeconds(timeInSecond);
        //     call?.Invoke();
        // }

        public YieldInstructionsQueue YieldInstructionsQueue(float interval, params IEnumerator[] enumerator)
        {
            var queue = new YieldInstructionsQueue(_host);
            queue.SetInterval(interval);
            foreach (var enumer in enumerator)
            {
#if UNITY_EDITOR
                queue.Push(LogableCoroutine(enumer));
#else
                queue.Push(enumer);
#endif
            }
            queue.Start();
            return queue;
        }

#endregion
    }
}