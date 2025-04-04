using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [DisallowMultipleComponent]
    public class SelectablePointer : SelectableInteractor, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Selectable target;
        /// <summary>
        /// onDown 之后触发
        /// </summary>
        public UnityEvent onDown = new UnityEvent();
        public UnityEvent onUp = new UnityEvent();
        public UnityEvent onSelected = new UnityEvent();
        public UnityEvent onDeselect = new UnityEvent();

        public void OnPointerDown(PointerEventData eventData)
        {
            onDown.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onUp.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onSelected.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onDeselect.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            onSelected.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            onDeselect.Invoke();
        }
    }
}