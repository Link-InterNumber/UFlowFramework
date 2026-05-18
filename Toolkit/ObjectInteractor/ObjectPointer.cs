using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    [DisallowMultipleComponent]
    public class ObjectPointer : ObjectInteractor, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// onDown 之后触发
        /// </summary>
        public UnityEvent onDown = new UnityEvent();
        public UnityEvent onUp = new UnityEvent();
        public UnityEvent onPointerEnter = new UnityEvent();
        public UnityEvent onPointerExit = new UnityEvent();

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
            onPointerEnter.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit.Invoke();
        }
    }
}