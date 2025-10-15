using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class TestActClip : ActClipData
    {
        private bool _isReady ;
        public override void Prepare(ActRuntimePlayer target, bool inEditor)
        {
            _isReady = true;
        }

        public override bool IsReady => _isReady;
        public override void OnStart(ActRuntimePlayer target)
        {
            Debug.Log("ActEditor: OnStart");
        }

        public override void DoAction(ActRuntimePlayer target, float time)
        {
            Debug.Log("ActEditor: DoAction "+ time);
        }

        public override void OnEnd(ActRuntimePlayer target)
        {
            Debug.Log("ActEditor: OnEnd");
        }

        public override void ReleaseAsset(ActRuntimePlayer target)
        {
            Debug.Log("ActEditor: ReleaseAsset");
        }

        public override Color editorColor => Color.cyan;
        public override string editorName => "Test Clip";
    }
}