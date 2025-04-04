using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    [DisallowMultipleComponent]
    public class SelectableScaler : SelectableInteractor, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public float enterScale = 1.1f;
        public float downScale = 1.2f;
        public float duration = 0.2f;

        private enum State
        {
            None,
            Scaling,
            ScaleUpNDown
        }
        
        private Vector3 _oriScale = Vector3.one;
        private State _scaling;

        private float _targetScale;
        private float _time;
        [SerializeField] private bool _isEnter;
        [SerializeField] private bool _isDown;
        protected override void Awake()
        {
            base.Awake();
            if (target) _oriScale = target.transform.localScale;
        }

        private void OnEnable()
        {
            _scaling = State.None;
            if (target) target.transform.localScale = _oriScale;
            target.Select();
            _isEnter = false;
            _isDown = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!target) return;
            _isDown = true;
            _isEnter = true;
            SetTargetScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!target || !_isEnter) return;
            _isDown = false;
            SetTargetScale();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!target) return;
            _isEnter = true;
            SetTargetScale();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!target) return;
            _isEnter = false;
            _isDown = false;
            SetTargetScale();
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            if (!target) return;
            _isEnter = true;
            SetTargetScale();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!target) return;
            _isEnter = false;
            SetTargetScale();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            if (!target) return;
            _scaling = State.ScaleUpNDown;
            _targetScale = downScale;
            _time = 0;
        }

        private void SetTargetScale()
        {
            _scaling = State.Scaling;
            _time = 0;
            if (!_isDown && !_isEnter)
            {
                _targetScale = 1f;
                return;
            }
            if (_isDown)
            {
                _targetScale = downScale;
                return;
            }
            _targetScale = enterScale;
        }

        private void Update()
        {
            switch (_scaling)
            {
                case State.None:
                    break;
                case State.Scaling:
                    _time += Time.unscaledDeltaTime;
                    target.transform.localScale = Vector3.Lerp(transform.localScale, _oriScale * _targetScale , _time / duration);
                    if (_time >= duration)
                    {
                        _scaling = State.None;
                        target.transform.localScale = _oriScale * _targetScale;
                    }
                    break;
                case State.ScaleUpNDown:
                    _time += Time.unscaledDeltaTime;
                    target.transform.localScale = Vector3.Lerp(transform.localScale, _oriScale * _targetScale , _time / (duration * 0.5f));
                    if (_time >= duration * 0.5f)
                    {
                        _scaling = State.Scaling;
                        _targetScale = enterScale;
                        _time = duration * 0.5f;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}