using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PowerCellStudio
{
    [DonotInitModuleAutoly]
    /// <summary>
    /// 异步系统统一入口，负责协程、调度、间隔任务、线程任务与 Job 的生命周期管理。
    /// </summary>
    public class AsyncManager : MonoSingleton<AsyncManager>
    {
        private CoroutineRunner _coroutineRunner;
        private SchedulerRunner _schedulerRunner;
        private IntervalCallRunner _intervalCallRunner;
        private ThreadedTasksRunner _threadedTasksRunner;
        private JobRunner _jobRunner;

        /// <summary>
        /// 获取协程执行器实例。
        /// </summary>
        public CoroutineRunner coroutineRunner => _coroutineRunner;

        /// <summary>
        /// 获取延时调度执行器实例。
        /// </summary>
        public SchedulerRunner schedulerRunner => _schedulerRunner;

        /// <summary>
        /// 获取间隔任务执行器实例。
        /// </summary>
        public IntervalCallRunner intervalCallRunner => _intervalCallRunner;

        /// <summary>
        /// 获取线程任务执行器实例。
        /// </summary>
        public ThreadedTasksRunner threadedTasksRunner => _threadedTasksRunner;

        /// <summary>
        /// 获取 Unity Job 执行器实例。
        /// </summary>
        public JobRunner jobRunner => _jobRunner;

        /// <summary>
        /// 确保异步管理器已创建并返回实例。
        /// </summary>
        /// <returns>异步管理器实例。</returns>
        public static AsyncManager EnsureInstance()
        {
            if (isExist && instance != null)
            {
                return instance;
            }

            var go = new GameObject(nameof(AsyncManager));
            return go.AddComponent<AsyncManager>();
        }

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            InitializeRunners();
        }

        private void InitializeRunners()
        {
            _coroutineRunner ??= new CoroutineRunner(this);
            _schedulerRunner ??= new SchedulerRunner();
            _intervalCallRunner ??= new IntervalCallRunner();
            _threadedTasksRunner ??= new ThreadedTasksRunner();
            _jobRunner ??= new JobRunner(_coroutineRunner);
        }

        /// <summary>
        /// 启动一个协程任务。
        /// </summary>
        /// <param name="routine">要执行的协程枚举器。</param>
        /// <returns>可用于等待或取消的异步句柄。</returns>
        public static AsyncHandlerBase Run(IEnumerator routine)
        {
            return EnsureInstance().RunCoroutine(routine);
        }

        /// <summary>
        /// 获取当前异步系统宿主对象。
        /// </summary>
        public static MonoBehaviour Host => EnsureInstance();

        /// <summary>
        /// 通过当前管理器实例启动一个协程任务。
        /// </summary>
        /// <param name="routine">要执行的协程枚举器。</param>
        /// <returns>可用于等待或取消的异步句柄。</returns>
        public AsyncHandlerBase RunCoroutine(IEnumerator routine)
        {
            return _coroutineRunner.RunCoroutine(routine);
        }

        /// <summary>
        /// 创建一个按顺序执行的协程队列。
        /// </summary>
        /// <param name="interval">相邻协程之间的间隔时间。</param>
        /// <param name="enumerators">待执行的协程枚举器列表。</param>
        /// <returns>协程队列对象。</returns>
        public YieldInstructionsQueue CreateYieldInstructionsQueue(float interval, params IEnumerator[] enumerators)
        {
            return _coroutineRunner.YieldInstructionsQueue(interval, enumerators);
        }

        /// <summary>
        /// 按时间延迟调度一个主线程任务。
        /// </summary>
        /// <param name="action">延迟后执行的回调。</param>
        /// <param name="delayTime">延迟秒数。</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放。</param>
        /// <returns>可用于等待或取消的异步句柄。</returns>
        public static AsyncHandlerBase Schedule(Action action, float delayTime = 0f, bool ignoreTimeScale = false)
        {
            return EnsureInstance().ScheduleTask(action, delayTime, ignoreTimeScale);
        }

        /// <summary>
        /// 通过当前管理器实例按时间延迟调度一个主线程任务。
        /// </summary>
        /// <param name="action">延迟后执行的回调。</param>
        /// <param name="delayTime">延迟秒数。</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放。</param>
        /// <returns>可用于等待或取消的异步句柄。</returns>
        public AsyncHandlerBase ScheduleTask(Action action, float delayTime = 0f, bool ignoreTimeScale = false)
        {
            return _schedulerRunner.ScheduleTask(action, delayTime, ignoreTimeScale);
        }

        /// <summary>
        /// 按帧数延迟调度一个主线程任务。
        /// </summary>
        /// <param name="action">延迟后执行的回调。</param>
        /// <param name="delayFrames">延迟帧数。</param>
        /// <returns>可用于等待或取消的异步句柄。</returns>
        public static AsyncHandlerBase ScheduleByFrame(Action action, int delayFrames = 0)
        {
            return EnsureInstance().ScheduleTaskByFrame(action, delayFrames);
        }

        /// <summary>
        /// 通过当前管理器实例按帧数延迟调度一个主线程任务。
        /// </summary>
        /// <param name="action">延迟后执行的回调。</param>
        /// <param name="delayFrames">延迟帧数。</param>
        /// <returns>可用于等待或取消的异步句柄。</returns>
        public AsyncHandlerBase ScheduleTaskByFrame(Action action, int delayFrames = 0)
        {
            return _schedulerRunner.ScheduleTaskByFrame(action, delayFrames);
        }

        /// <summary>
        /// 注册一个固定间隔执行的任务。
        /// </summary>
        /// <param name="interval">执行间隔秒数。</param>
        /// <param name="action">每次触发时执行的回调。</param>
        /// <param name="autoReleaseCondition">返回 true 时自动注销任务的条件。</param>
        /// <returns>任务索引，可用于后续注销。</returns>
        public int RegisterIntervalTask(float interval, Action action, Func<bool> autoReleaseCondition = null)
        {
            return _intervalCallRunner.RegisterTask(interval, action, autoReleaseCondition);
        }

        /// <summary>
        /// 注销一个已注册的间隔任务。
        /// </summary>
        /// <param name="taskIndex">任务索引。</param>
        public void UnregisterIntervalTask(int taskIndex)
        {
            _intervalCallRunner.UnregisterTask(taskIndex);
        }

        /// <summary>
        /// 清空当前所有间隔任务。
        /// </summary>
        public void ClearIntervalTasks()
        {
            _intervalCallRunner.ClearAllTasks();
        }

        /// <summary>
        /// 在线程池中执行后台任务，并在主线程回调完成通知。
        /// </summary>
        /// <param name="backgroundTask">后台线程执行的任务。</param>
        /// <param name="mainThreadCallback">后台任务结束后在主线程执行的回调。</param>
        public void RunTaskAsync(Action backgroundTask, Action mainThreadCallback = null)
        {
            _threadedTasksRunner.RunTaskAsync(backgroundTask, mainThreadCallback);
        }

        /// <summary>
        /// 在线程池中执行带参数的后台任务，并在主线程回调完成通知。
        /// </summary>
        /// <typeparam name="T">后台任务参数类型。</typeparam>
        /// <param name="backgroundTask">后台线程执行的任务。</param>
        /// <param name="parameter">传递给后台任务的参数。</param>
        /// <param name="mainThreadCallback">后台任务结束后在主线程执行的回调。</param>
        public void RunTaskAsync<T>(Action<T> backgroundTask, T parameter, Action mainThreadCallback = null)
        {
            _threadedTasksRunner.RunTaskAsync(backgroundTask, parameter, mainThreadCallback);
        }

        /// <summary>
        /// 同步执行一个 Unity Job，并阻塞直到完成。
        /// </summary>
        /// <typeparam name="TJob">Job 类型。</typeparam>
        /// <param name="job">要执行的 Job。</param>
        public void SyncRunJob<TJob>(TJob job)
            where TJob : struct, IJob
        {
            _jobRunner.SyncRunJob(job);
        }

        /// <summary>
        /// 异步执行一个 Unity Job，完成后返回结果数组。
        /// </summary>
        /// <typeparam name="TJob">Job 类型。</typeparam>
        /// <typeparam name="T">结果数组元素类型。</typeparam>
        /// <param name="job">要执行的 Job。</param>
        /// <param name="resultArray">用于承接结果的原生数组。</param>
        /// <param name="onComplete">任务完成后的回调。</param>
        public void AsyncRunJob<TJob, T>(TJob job, NativeArray<T> resultArray, Action<NativeArray<T>> onComplete)
            where TJob : struct, IJob
            where T : struct
        {
            _jobRunner.AsyncRunJob(job, resultArray, onComplete);
        }

        /// <summary>
        /// 同步执行一个并行 Unity Job，并阻塞直到完成。
        /// </summary>
        /// <typeparam name="TJob">Job 类型。</typeparam>
        /// <param name="job">要执行的 Job。</param>
        /// <param name="length">并行处理的数据长度。</param>
        /// <param name="batchCount">每批处理数量。</param>
        public void SyncRunParallelJob<TJob>(TJob job, int length, int batchCount = 64)
            where TJob : struct, IJobFor
        {
            _jobRunner.SyncRunParallelJob(job, length, batchCount);
        }

        /// <summary>
        /// 异步执行一个并行 Unity Job，完成后返回结果数组。
        /// </summary>
        /// <typeparam name="TJob">Job 类型。</typeparam>
        /// <typeparam name="T">结果数组元素类型。</typeparam>
        /// <param name="job">要执行的 Job。</param>
        /// <param name="resultArray">用于承接结果的原生数组。</param>
        /// <param name="length">并行处理的数据长度。</param>
        /// <param name="onComplete">任务完成后的回调。</param>
        /// <param name="batchCount">每批处理数量。</param>
        public void AsyncRunParallelJob<TJob, T>(TJob job, NativeArray<T> resultArray, int length,
            Action<NativeArray<T>> onComplete, int batchCount = 64)
            where TJob : struct, IJobFor
            where T : struct
        {
            _jobRunner.AsyncRunParallelJob(job, resultArray, length, onComplete, batchCount);
        }

        private void Update()
        {
            float time = Time.time;
            float unscaledTime = Time.unscaledTime;
            int frameCount = Time.frameCount;
            _schedulerRunner?.Tick(time, unscaledTime, frameCount);
            _threadedTasksRunner?.Tick();
            _jobRunner?.Tick();
        }

        private void FixedUpdate()
        {
            _intervalCallRunner?.Tick(Time.fixedDeltaTime);
        }

        protected override void Deinit()
        {
            base.Deinit();
            _jobRunner?.Dispose();
            _threadedTasksRunner?.Dispose();
            _intervalCallRunner?.Dispose();
            _schedulerRunner?.Dispose();
            _coroutineRunner?.Dispose();
            _coroutineRunner = null;
            _schedulerRunner = null;
            _intervalCallRunner = null;
            _threadedTasksRunner = null;
            _jobRunner = null;
        }
    }
}