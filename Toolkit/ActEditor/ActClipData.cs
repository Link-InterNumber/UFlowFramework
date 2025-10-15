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

        public abstract void Prepare(ActRuntimePlayer target, bool inEditor);

        public abstract bool IsReady { get; }

        public abstract void OnStart(ActRuntimePlayer target);

        public abstract void DoAction(ActRuntimePlayer target, float time);

        public abstract void OnEnd(ActRuntimePlayer target);

        public abstract void ReleaseAsset(ActRuntimePlayer target);

        public abstract Color editorColor { get; }

        public abstract string editorName { get; }
    }
}

