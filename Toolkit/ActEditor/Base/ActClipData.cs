using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public abstract class ActClipData
    {
        public string id = Guid.NewGuid().ToString();
        public float start = 0f;
        public float length = 1f;
        public float duration => start + length;

        protected float GetNormalizedTime(float time) => Mathf.Clamp01((time - start) / length);

        protected float GetProcessTime(float time) => Mathf.Clamp(time - start, 0f, length);

        protected float lastTime;

        public void Simulate(ActRuntimePlayer target, float time)
        {
            if (lastTime < start || lastTime > start + length)
            {
                if (time >= start && time <= start + length)
                {
                    OnStart(target);
                    DoAction(target, time);
                }
            }
            else
            {
                if (time < start || time > start + length)
                {
                    DoAction(target, time < start ? start : start + length);
                    OnEnd(target);
                }
                else
                {
                    DoAction(target, time);
                }
            }
            lastTime = time;
        }
        public abstract Color editorColor { get; }

        public abstract string editorName { get; }

        public abstract bool IsReady { get; }

        public abstract void Prepare(ActRuntimePlayer target, bool inEditor);

        public abstract void ReleaseAsset(ActRuntimePlayer target);

        protected abstract void OnStart(ActRuntimePlayer target);

        protected abstract void DoAction(ActRuntimePlayer target, float time);

        protected abstract void OnEnd(ActRuntimePlayer target);
    }
}

