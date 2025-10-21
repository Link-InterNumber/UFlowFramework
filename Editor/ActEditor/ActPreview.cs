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
        public bool Loop;
        public float Speed = 1f;
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
        }

        public void SetTarget(ActRuntimePlayer target)
        {
            if (Target == target) return;

            Stop();
            EndAll();
            UnloadAll();

            Target = target;
        }

        public void Play()
        {
            if (Asset == null || Target == null) return;
            if (Speed <= 0f) Speed = 1f;
            UnloadAll();
            PrepareAll();
            IsPlaying = true;
            CurrentTime = 0;
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

        // 主评估：进入/离开片段并执行动作
        public void EvaluateAt(float time)
        {
            CurrentTime = time;
            if (Asset == null || Target == null) return;
            Asset.EvaluateAt(time, Target);
            // if (isEnd && !Loop) IsPlaying = false;
            // else if (isEnd) Asset.Restart();
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
            CurrentTime = t;
            if (Asset == null || Target == null) return;
            Asset.Simulate(dt, Target, out var isEnd);
            if (isEnd && !Loop) IsPlaying = false;
            else if (isEnd) Asset.Restart();
            // EvaluateAt(t, dt * Mathf.Max(0.0001f, Speed));
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
                        clip.Prepare(Target, null, true);
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
                    clip.Simulate(Target, 999999);
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