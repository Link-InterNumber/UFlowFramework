using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleIAutoly]
    public class ThreadedTasksHandler : MonoSingleton<ThreadedTasksHandler>
    {
        private static ThreadedTasksHandler _instance;
        private ConcurrentQueue<System.Action> _mainThreadActions = new ConcurrentQueue<System.Action>();

        void Update()
        {
            // 在主线程执行所有回调
            while (_mainThreadActions.TryDequeue(out System.Action action))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// 将任务提交到线程池执行
        /// </summary>
        /// <param name="backgroundTask">后台任务逻辑</param>
        /// <param name="mainThreadCallback">主线程回调（可选）</param>
        public void RunTaskAsync(System.Action backgroundTask, System.Action mainThreadCallback = null)
        {
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
                    Debug.LogError($"Task failed: {ex}");
                }
            });
        }

        // 添加带参数的版本
        public void RunTaskAsync<T>(System.Action<T> backgroundTask, T parameter, System.Action mainThreadCallback = null)
        {
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
                    Debug.LogError($"Task failed: {ex}");
                }
            });
        }

    }
}

