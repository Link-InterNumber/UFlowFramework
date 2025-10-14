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

        public abstract void OnLoad();
        public abstract bool IsReady { get; }

        public abstract void OnStart(ActRuntimePlayer target);

        public abstract void DoAction(ActRuntimePlayer target, float time);

        public abstract void OnEnd(ActRuntimePlayer target);

        public abstract Color editorColor { get; }

        public abstract string editorName { get; }
    }
}

