using System;
using UnityEngine;

namespace PowerCellStudio
{
    public class ScaleTweenShowUp : MonoBehaviour
    {
        public EaseType ease = EaseType.OutBack;
        public float duration = 0.3f;

        private float _currentTime;
        private bool _inTween;

        private void OnEnable()
        {
            transform.localScale = Vector3.zero;
            _currentTime = 0;
            _inTween = true;
        }

        private void Update()
        {
            if (!_inTween) return;
            var normalizeTime = _currentTime / duration;
            transform.localScale = Vector3.one * Ease.GetEase(ease, normalizeTime);
            _currentTime += Time.unscaledDeltaTime;
            if (_currentTime >= duration)
            {
                transform.localScale = Vector3.one;
                _inTween = false;
            }
        }
    }
}