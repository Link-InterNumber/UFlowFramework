using System;
using System.Collections;
using LinkFrameWork.MonoInstance;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Res.Scripts.UI.PlayerCtrlUi
{
    public class HoldButton: MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent<bool> onPointChange = new UnityEvent<bool>();

        private float _holdTime = 0;
        public float HoldTime => _isPressing ? _holdTime : 0;
        
        private bool _isPressing = false;
        public bool IsPressing => _isPressing;

        private bool _isPressThisFrame = false;
        private Coroutine _co;

        public bool IsPressThisFrame => _isPressing && _isPressThisFrame;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressThisFrame = true;
            onPointChange?.Invoke(true);
            _isPressing = true;
            _holdTime = 0;
            // ApplicationManager.Instance.StartCoroutine(DePressThisFrame());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onPointChange?.Invoke(false);
            _isPressing = false;
        }

        private WaitForEndOfFrame _endOfFrame;

        private void LateUpdate()
        {
            _isPressThisFrame = false;
        }

        private void Update()
        {
            if(_isPressing)
                _holdTime += Time.deltaTime;
        }
    }
}