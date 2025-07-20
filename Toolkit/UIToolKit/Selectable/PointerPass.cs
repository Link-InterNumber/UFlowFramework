using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class PointerPass : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public GameObject passTarget;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!passTarget) return;
            UIUtils.PassEvent(eventData, ExecuteEvents.pointerClickHandler, passTarget);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!passTarget) return;
            UIUtils.PassEvent(eventData, ExecuteEvents.pointerDownHandler, passTarget);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!passTarget) return;
            UIUtils.PassEvent(eventData, ExecuteEvents.submitHandler, passTarget);
            UIUtils.PassEvent(eventData, ExecuteEvents.pointerUpHandler, passTarget);
        }
    }
}