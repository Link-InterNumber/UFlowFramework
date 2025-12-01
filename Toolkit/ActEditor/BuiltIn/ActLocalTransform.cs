using System;
using UnityEngine;

namespace PowerCellStudio
{
    [Serializable]
    public class ActLocalTransform : ActClipData
    {
        public AssetPath<AnimationClip> clip = new AssetPath<AnimationClip>();

        private AnimationClip _loadedClip;

        public override bool IsReady => _loadedClip;

        public override Color editorColor => new Color(0.6f, 0.9f, 0.3f);

        public override string editorName => "LocalTransform";

        public override void Prepare(ActRuntimePlayer target, IAssetLoader assetloader, bool inEditor)
        {
            if (string.IsNullOrEmpty(clip.assetPath)) return;
            if (inEditor)
            {
#if UNITY_EDITOR
                _loadedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(clip.assetPath);
#endif
                return;
            }
            assetloader.LoadAsync<AnimationClip>(clip.assetPath, (a) => _loadedClip = a);
        }

        public override void ReleaseAsset(ActRuntimePlayer target)
        {
            _loadedClip = null;
        }

        private Transform _sampleTarget;
        private Vector3 _sampleLastPosition;
        private Quaternion _sampleLastRotation;
        private Vector3 _sampleLastScale;
        
        private Quaternion _rotation;
        protected override void OnStart(ActRuntimePlayer target)
        {
            if (!target) return;
            _sampleTarget = new GameObject($"[ActSample]{target.name}").transform;
            var startTime = lastTime < start ? 0 : _loadedClip.length;
            _loadedClip.SampleAnimation(_sampleTarget.gameObject, startTime);
            _sampleLastPosition = _sampleTarget.localPosition;
            _sampleLastRotation = _sampleTarget.localRotation;
            _sampleLastScale = _sampleTarget.localScale;

            // var targetForward = target.transform.rotation * Vector3.forward;
            _rotation = target.transform.rotation;
        }

        protected override void DoAction(ActRuntimePlayer target, float time)
        {
            var normalizedTime = GetNormalizedTime(time);
            _loadedClip.SampleAnimation(_sampleTarget.gameObject, normalizedTime * _loadedClip.length);
            var deltaPosition = _sampleTarget.localPosition - _sampleLastPosition;
            var deltaRotation = _sampleTarget.localRotation * Quaternion.Inverse(_sampleLastRotation);
            var scale = new Vector3(
                _sampleTarget.localScale.x / Mathf.Max(1e-6f, _sampleLastScale.x),
                _sampleTarget.localScale.y / Mathf.Max(1e-6f, _sampleLastScale.y),
                _sampleTarget.localScale.z / Mathf.Max(1e-6f, _sampleLastScale.z));

            // 根据 target 的朝向
            target.transform.position += _rotation * deltaPosition;
            target.transform.rotation = deltaRotation * target.transform.rotation;
            target.transform.localScale = Vector3.Scale(target.transform.localScale, scale);

            _sampleLastPosition = _sampleTarget.localPosition;
            _sampleLastRotation = _sampleTarget.localRotation;
            _sampleLastScale = _sampleTarget.localScale;
        }

        protected override void OnEnd(ActRuntimePlayer target)
        {
            if (Application.isPlaying)
            {
                if (_sampleTarget) GameObject.Destroy(_sampleTarget.gameObject);
                _sampleTarget = null;
                return;
            }
            if (_sampleTarget) GameObject.DestroyImmediate(_sampleTarget.gameObject);
            _sampleTarget = null;
        }
    }
}