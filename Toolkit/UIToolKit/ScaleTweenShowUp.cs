using System;
using UnityEngine;

namespace PowerCellStudio
{
    public class ScaleTweenShowUp : MonoBehaviour
    {
        public EaseType ease = EaseType.OutBack;
        public float duration = 0.3f;

        private float _normalizedTime;
        private bool _inTween;

        private void OnEnable()
        {
            transform.localScale = Vector3.zero;
            _normalizedTime = 0;
            _inTween = true;
        }

        private void Update()
        {
            if (!_inTween) return;
            transform.localScale = Vector3.one * Ease.GetEase(ease, _normalizedTime);
            _normalizedTime += Time.deltaTime / duration;
            if (_normalizedTime >= 1f)
            {
                transform.localScale = Vector3.one;
                _inTween = false;
            }
        }
    }
}