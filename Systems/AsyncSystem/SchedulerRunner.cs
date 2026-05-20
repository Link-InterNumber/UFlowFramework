using System;
using UnityEngine;

namespace PowerCellStudio
{
    public sealed class SchedulerRunner : IDisposable
    {
        private PoolableObjectPool<SchedulerTask> _taskPool = new PoolableObjectPool<SchedulerTask>(() => new SchedulerTask(), 20, 10);

        private System.Collections.Generic.List<SchedulerTask> tasks = new System.Collections.Generic.List<SchedulerTask>();

        public void Dispose()
        {
            _taskPool.Dispose();
            foreach (var task in tasks)
            {
                task.Dispose();
            }
            tasks.Clear();
            _taskPool = null;
            tasks = null;
        }

        public AsyncHandlerBase ScheduleTask(System.Action action, float delayTime = 0f, bool ignoreTimeScale = false)
        {
            if (action == null) return null;
            if (delayTime <= 0f)
            {
                action.Invoke();
                return null;
            }
            SchedulerTask task = _taskPool.Get();
            task.action = action;
            task.delayTime = delayTime;
            task.delayFrames = 0;
            task.byFrame = false;
            task.ignoreTimeScale = ignoreTimeScale;
            task.startFrame = Time.frameCount;
            task.startTime = ignoreTimeScale ? Time.unscaledTime : Time.time;
            tasks.Add(task);
            return task;
        }

        public AsyncHandlerBase ScheduleTaskByFrame(System.Action action, int delayFrames = 0)
        {
            if (action == null) return null;
            if (delayFrames <= 0)
            {
                action.Invoke();
                return null;
            }
            SchedulerTask task = _taskPool.Get();
            task.action = action;
            task.delayTime = 0f;
            task.delayFrames = delayFrames;
            task.byFrame = true;
            task.ignoreTimeScale = false;
            task.startFrame = Time.frameCount;
            task.startTime = Time.time;
            tasks.Add(task);
            return task;
        }


        public void Tick(float time, float unscaledTime, int frameCount)
        {
            for (var i = 0; i < tasks.Count;)
            {
                var task = tasks[i];
                if (task == null)
                {
                    tasks.RemoveAt(i);
                    continue;
                }
                if (task.cancelled)
                {
                    task.DeSpawn();
                    tasks.RemoveAt(i);
                    continue;
                }
                if (task.byFrame)
                {
                    if (frameCount - task.startFrame >= task.delayFrames)
                    {
                        task.action?.Invoke();
                        task.DeSpawn();
                        tasks.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    float elapsedTime = task.ignoreTimeScale ? unscaledTime - task.startTime : time - task.startTime;
                    if (elapsedTime >= task.delayTime)
                    {
                        task.action?.Invoke();
                        task.DeSpawn();
                        tasks.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
        
    }
}