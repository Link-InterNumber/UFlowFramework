using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PowerCellStudio
{
    /// 负责在编辑器里预览 ActAsset：
    //// - 控制播放、暂停、停止、速度、循环
    //// - 在时间推进或擦洗时，正确调用各片段的 Prepare/OnStart/DoAction/OnEnd
    public sealed class ActPreview : IDisposable
    {
        public ActAsset Asset { get; private set; }
        public ActRuntimePlayer Target { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool Loop { get; set; } = true;
        public float Speed { get; set; } = 1f;
        public float CurrentTime { get; private set; }
        public float Duration { get; private set; }
        
#if UNITY_EDITOR
        private double _lastEditorTime;
#endif

        public ActPreview(ActAsset asset = null, ActRuntimePlayer target = null)
        {
            SetAsset(asset);
            SetTarget(target);
#if UNITY_EDITOR
            EditorApplication.update += EditorTick;
#endif
        }

        public void Dispose()
        {
            Stop();
            UnloadAll();
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
#endif
        }

        public void SetAsset(ActAsset asset)
        {
            if (Asset == asset) return;

            Stop();
            EndAll();
            UnloadAll();

            Asset = asset;
            PrepareAll();
        }

        public void SetTarget(ActRuntimePlayer target)
        {
            if (Target == target) return;

            Stop();
            EndAll();
            UnloadAll();

            Target = target;
            PrepareAll();
        }

        public void Play()
        {
            if (Asset == null || Target == null) return;
            if (Speed <= 0f) Speed = 1f;
            IsPlaying = true;
            Asset.Restart();
#if UNITY_EDITOR
            _lastEditorTime = 0;
#endif
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Stop()
        {
            IsPlaying = false;
            EndAll();
        }

        public void Seek(float time)
        {
            time = Mathf.Max(0f, time);
            if (Duration > 0f && !Loop) time = Mathf.Min(time, Duration);
            EvaluateAt(time, 0f);
        }

        // 主评估：进入/离开片段并执行动作
        private void EvaluateAt(float time, float dt)
        {
            CurrentTime = time;
            if (Asset == null || Target == null) return;
            Asset.Simulate(dt, Target, out var isEnd);
            if (isEnd && !Loop) IsPlaying = false;
            else if (isEnd) Asset.Restart();
        }

#if UNITY_EDITOR
        private void EditorTick()
        {
            if (!IsPlaying || Asset == null || Target == null) return;

            var now = EditorApplication.timeSinceStartup;
            if (_lastEditorTime <= 0) _lastEditorTime = now;
            var dt = (float)(now - _lastEditorTime);
            _lastEditorTime = now;

            var t = CurrentTime + dt * Mathf.Max(0.0001f, Speed);

            if (Loop && Duration > 0f)
            {
                t %= Duration;
                if (t < 0f) t += Duration;
            }
            else
            {
                t = Mathf.Clamp(t, 0f, Mathf.Max(0.0001f, Duration));
                IsPlaying = t < Duration;
            }

            EvaluateAt(t, dt * Mathf.Max(0.0001f, Speed));
        }
#endif

        private void PrepareAll()
        {
            if (Asset == null || Target == null) return;
            Duration = Asset.duration;

            foreach (var c in Asset.tracks)
            {
                foreach (var clip in c.clips)
                {
                    try
                    {
                        clip.Prepare(Target, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private void UnloadAll()
        {
            if (Asset == null || Target == null) return;
            foreach (var c in Asset.tracks)
            {
                foreach (var clip in c.clips)
                    SafeRelease(clip);
            }
        }

        private void EndAll()
        {
            if (Asset == null || Target == null) return;
            foreach (var c in Asset.tracks)
            {
                foreach (var clip in c.clips)
                    SafeOnEnd(clip);
            }
        }

        // private void SafePrepare(ActClipData c)
        // {
        //     if (c == null) return;
        //     try
        //     {
        //         c.Prepare(Target, true);
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogException(ex);
        //     }
        // }

        // private void SafeOnStart(ActClipData c)
        // {
        //     if (c == null) return;
        //     try
        //     {
        //         c.OnStart(Target);
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogException(ex);
        //     }
        // }

        // private void SafeDoAction(ActClipData c, float time)
        // {
        //     if (c == null) return;
        //     try
        //     {
        //         c.DoAction(Target, time);
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.LogException(ex);
        //     }
        // }

        private void SafeOnEnd(ActClipData c)
        {
            if (c == null) return;
            try
            {
                c.OnEnd(Target);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void SafeRelease(ActClipData c)
        {
            if (c == null) return;
            try
            {
                c.ReleaseAsset(Target);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}