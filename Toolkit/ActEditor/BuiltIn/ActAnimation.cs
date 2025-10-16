using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PowerCellStudio
{
    [Serializable]
    public class ActAnimation : ActClipData
    {
        public AssetPath<AnimationClip> clip = new AssetPath<AnimationClip>();

        public bool loopMode;

        public bool sampleAnimation;

        public override bool IsReady => _loadedClip;

        public override Color editorColor => new Color(0.3f, 0.6f, 0.9f);

        public override string editorName => "ActAnimation";

        private AnimationClip _loadedClip;

        public override void Prepare(ActRuntimePlayer target, bool inEditor)
        {
            if (string.IsNullOrEmpty(clip.assetPath)) return;
            if (inEditor)
            {
#if UNITY_EDITOR
                _loadedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(clip.assetPath);
#endif
                return;
            }
            _loadedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(clip.assetPath);
        }

        private PlayableGraph _playableGraph;
        private AnimationClipPlayable _clipPlayable;
        private RuntimeAnimatorController _originalController;

        protected override void OnStart(ActRuntimePlayer target)
        {
            var animator = target.GetComponent<Animator>();
            if (animator == null) return;
            if (sampleAnimation)
            {
                animator.enabled = false;
                return;
            }
            _originalController = animator.runtimeAnimatorController;
            animator.runtimeAnimatorController = null;
            _playableGraph = PlayableGraph.Create();
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _clipPlayable = AnimationClipPlayable.Create(_playableGraph, _loadedClip);
            var output = AnimationPlayableOutput.Create(_playableGraph, "AnimationOutput", animator);
            output.SetSourcePlayable(_clipPlayable);

        }

        protected override void DoAction(ActRuntimePlayer target, float time)
        {
            var t = time - start;
            if (loopMode)
            {
                var processTime = GetProcessTime(time);
                t = processTime % _loadedClip.length;
            }
            else
            {
                var normalizedTime = GetNormalizedTime(time);
                t = normalizedTime * _loadedClip.length;
            }
            if (_playableGraph.IsValid())
            {
                _clipPlayable.SetTime(t);
                _playableGraph.Evaluate(0f);
            }
            else if (sampleAnimation)
            {
                _loadedClip.SampleAnimation(target.gameObject, t);
            }

        }

        protected override void OnEnd(ActRuntimePlayer target)
        {
            var animator = target.GetComponent<Animator>();
            if (animator) return;
            if (animator.enabled == false) animator.enabled = true;
            animator.runtimeAnimatorController = _originalController;
        }

        public override void ReleaseAsset(ActRuntimePlayer target)
        {
            if (_playableGraph.IsValid())
            {
                _playableGraph.Destroy();
                _playableGraph = default;
                _clipPlayable = default;
            }
            _loadedClip = null;
            _originalController = null;
        }
    }
}

