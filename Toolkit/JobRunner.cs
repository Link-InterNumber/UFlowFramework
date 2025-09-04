using System;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PowerCellStudio
{
    public class JobRunner : MonoBehaviour
    {
#if !UNITY_WEBGL
        private struct JobInfo
        {
            public JobHandle handle;
            public Action onComplete;
        }

        private List<JobInfo> _asyncJobs = new List<JobInfo>();
        private List<int> _removeBuffer = new List<int>();
#endif

        /// <summary>
        /// 同步执行IJob类型的Job，立即在主线程完成计算。
        /// Synchronously runs an IJob type job and completes calculation on the main thread.
        /// </summary>
        /// <typeparam name="TJob">Job结构体类型 / Job struct type</typeparam>
        /// <typeparam name="T">结果类型 / Result type</typeparam>
        /// <param name="job">要执行的Job实例 / Job instance to execute</param>
        public void SyncRunJob<TJob, T>(TJob job)
           where TJob : struct, IJob
           where T : struct
        {
#if UNITY_WEBGL
            // WebGL不支持JobSystem，直接返回原始结果
            job.Execute();
#else
            JobHandle handle = job.Schedule();
            handle.Complete();
#endif
        }

        /// <summary>
        /// 异步执行IJob类型的Job，结果在回调中返回。
        /// Asynchronously runs an IJob type job, result is returned via callback.
        /// </summary>
        /// <typeparam name="TJob">Job结构体类型 / Job struct type</typeparam>
        /// <typeparam name="T">结果类型 / Result type</typeparam>
        /// <param name="job">要执行的Job实例 / Job instance to execute</param>
        /// <param name="resultArray">结果数组 / Result array</param>
        /// <param name="onComplete">完成时回调 / Callback when job is complete</param>
        public void AsyncRunJob<TJob, T>(TJob job, NativeArray<T> resultArray, Action<NativeArray<T>> onComplete)
           where TJob : struct, IJob
           where T : struct
        {
#if UNITY_WEBGL
            job.Execute();
            onComplete?.Invoke(resultArray);
#else
            JobHandle handle = job.Schedule();
            _asyncJobs.Add(new JobInfo
            {
                handle = handle,
                onComplete = () =>
                {
                    var result = resultArray;
                    onComplete?.Invoke(result);
                }
            });
#endif
        }

        /// <summary>
        /// 同步执行IJobFor类型的并行Job，立即在主线程完成计算。
        /// Synchronously runs an IJobFor type parallel job and completes calculation on the main thread.
        /// </summary>
        /// <typeparam name="TJob">Job结构体类型 / Job struct type</typeparam>
        /// <typeparam name="T">结果类型 / Result type</typeparam>
        /// <param name="job">要执行的Job实例 / Job instance to execute</param>
        /// <param name="length">并行长度 / Parallel length</param>
        /// <param name="batchCount">每批处理数量 / Batch count per schedule</param>
        public void SyncRunParallelJob<TJob>(TJob job, int length, int batchCount = 64)
           where TJob : struct, IJobFor
        {
#if UNITY_WEBGL
            // WebGL不支持JobSystem，直接返回
            for (int i = 0; i < length; i++)
            {
                job.Execute(i);
            }
            return;
#else
            JobHandle handle = job.ScheduleParallel(length, batchCount, default);
            handle.Complete();
#endif
        }

        /// <summary>
        /// 异步执行IJobFor类型的并行Job，结果在回调中返回。
        /// Asynchronously runs an IJobFor type parallel job, result is returned via callback.
        /// </summary>
        /// <typeparam name="TJob">Job结构体类型 / Job struct type</typeparam>
        /// <typeparam name="T">结果类型 / Result type</typeparam>
        /// <param name="job">要执行的Job实例 / Job instance to execute</param>
        /// <param name="resultArray">结果数组 / Result array</param>
        /// <param name="length">并行长度 / Parallel length</param>
        /// <param name="onComplete">完成时回调 / Callback when job is complete</param>
        /// <param name="batchCount">每批处理数量 / Batch count per schedule</param>
        public void AsyncRunParallelJob<TJob, T>(TJob job, NativeArray<T> resultArray, int length, Action<NativeArray<T>> onComplete, int batchCount = 64)
           where TJob : struct, IJobFor
           where T : struct
        {
#if UNITY_WEBGL
            // WebGL不支持JobSystem，直接回调
            ApplicationManager.RunCoroutine(AsyncRunParallelJobCorount(job, resultArray, length, onComplete, batchCount));
#else
            JobHandle handle = job.ScheduleParallel(length, batchCount, default);
            _asyncJobs.Add(new JobInfo
            {
                handle = handle,
                onComplete = () => onComplete?.Invoke(resultArray)
            });
#endif
        }

        /// <summary>
        /// WebGL平台下异步模拟并行Job的协程实现。
        /// Coroutine implementation for async parallel job simulation on WebGL platform.
        /// </summary>
        /// <typeparam name="TJob">Job结构体类型 / Job struct type</typeparam>
        /// <typeparam name="T">结果类型 / Result type</typeparam>
        /// <param name="job">要执行的Job实例 / Job instance to execute</param>
        /// <param name="resultArray">结果数组 / Result array</param>
        /// <param name="length">并行长度 / Parallel length</param>
        /// <param name="onComplete">完成时回调 / Callback when job is complete</param>
        /// <param name="batchCount">每批处理数量 / Batch count per schedule</param>
        private IEnumerator AsyncRunParallelJobCorount<TJob, T>(TJob job, NativeArray<T> resultArray, int length, Action<NativeArray<T>> onComplete, int batchCount)
           where TJob : struct, IJobFor
           where T : struct
        {
            var frameCount = 0;
            while (frameCount <= Mathf.CeilToInt(length * 1f / batchCount))
            {
                for (int i = 0; i < batchCount; i++)
                {
                    var index = i + frameCount * batchCount;
                    if (index >= length) break;
                    job.Execute(index);
                }
                frameCount++;
                yield return null;
            }
            onComplete?.Invoke(resultArray);
        }

#if !UNITY_WEBGL
        private void Update()
        {
            // WebGL不支持JobSystem，无需处理异步Job
            // 检查异步Job是否完成
            for (int i = 0; i < _asyncJobs.Count; i++)
            {
                var jobInfo = _asyncJobs[i];
                if (jobInfo.handle.IsCompleted)
                {
                    jobInfo.handle.Complete();
                    jobInfo.onComplete?.Invoke();
                    _removeBuffer.Add(i);
                }
            }
            // 清理已完成的Job
            if (_removeBuffer.Count == 0) return;
            for (int i = _removeBuffer.Count - 1; i >= 0; i--)
            {
                int index = _removeBuffer[i];
                _asyncJobs.RemoveAt(index);
            }
            _removeBuffer.Clear();
        }

        void OnDestroy()
        {
            // 清理所有异步Job
            foreach (var jobInfo in _asyncJobs)
            {
                if (jobInfo.handle.IsCompleted)
                {
                    jobInfo.handle.Complete();
                    jobInfo.onComplete?.Invoke();
                }
            }
            _asyncJobs.Clear();
            _removeBuffer.Clear();
        }
#endif
    }
}
