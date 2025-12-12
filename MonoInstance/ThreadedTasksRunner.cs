#if !UNITY_WEBGL
using System.Collections.Concurrent;
using System.Threading;
#endif
using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleAutoly]
    public class ThreadedTasksRunner : MonoSingleton<ThreadedTasksRunner>
    {
#if !UNITY_WEBG
        private ConcurrentQueue<System.Action> _mainThreadActions = new ConcurrentQueue<System.Action>();

        void Update()
        {
            // 在主线程执行所有回调
            while (_mainThreadActions.TryDequeue(out System.Action action))
            {
                action?.Invoke();
            }
        }
#endif

        /// <summary>
        /// Submits a task to the thread pool for execution.
        /// 将任务提交到线程池执行。
        /// </summary>
        /// <param name="backgroundTask">Background task logic.
        /// 后台任务逻辑。</param>
        /// <param name="mainThreadCallback">Main thread callback (optional).
        /// 主线程回调（可选）。</param>
        public void RunTaskAsync(System.Action backgroundTask, System.Action mainThreadCallback = null)
        {
#if UNITY_WEBGL
            backgroundTask?.Invoke();
            mainThreadCallback?.Invoke();
#else
            ThreadPool.QueueUserWorkItem(_ => 
            {
                try
                {
                    // 执行后台任务
                    backgroundTask?.Invoke();

                    // 如果有回调则加入主线程队列
                    if (mainThreadCallback != null)
                    {
                        _mainThreadActions.Enqueue(mainThreadCallback);
                    }
                }
                catch (System.Exception ex)
                {
                    _mainThreadActions.Enqueue(()=>Debug.LogError($"Task failed: {ex}"));
                }
            });
#endif
        }

        /// <summary>
        /// Submits a task to the thread pool for execution.
        /// 将任务提交到线程池执行。
        /// </summary>
        /// <param name="backgroundTask">Background task logic.
        /// 后台任务逻辑。</param>
        /// <param name="parameter"> The input of background task
        /// 后台任务需要的输入参数。</param>
        /// <param name="mainThreadCallback">Main thread callback (optional).
        /// 主线程回调（可选）。</param>
        public void RunTaskAsync<T>(System.Action<T> backgroundTask, T parameter, System.Action mainThreadCallback = null)
        {
#if UNITY_WEBGL
            backgroundTask?.Invoke(parameter);
            mainThreadCallback?.Invoke();
#else
            ThreadPool.QueueUserWorkItem(_ => 
            {
                try
                {
                    backgroundTask?.Invoke(parameter);
                    if (mainThreadCallback != null)
                    {
                        _mainThreadActions.Enqueue(mainThreadCallback);
                    }
                }
                catch (System.Exception ex)
                {
                    _mainThreadActions.Enqueue(()=>Debug.LogError($"Task failed: {ex}"));
                }
            });
#endif
        }

    }
}

