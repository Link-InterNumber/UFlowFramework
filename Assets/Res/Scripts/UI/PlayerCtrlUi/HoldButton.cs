using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Res.Scripts.UI.PlayerCtrlUi
{
    public class HoldButton: MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent<bool> onPointChange = new UnityEvent<bool>();

        private bool _isPressing = false;
        public bool IsPressing => _isPressing;

        public void OnPointerDown(PointerEventData eventData)
        {
            onPointChange?.Invoke(true);
            _isPressing = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onPointChange?.Invoke(false);
            _isPressing = false;
        }
    }
}