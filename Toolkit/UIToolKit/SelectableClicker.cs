using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class SelectableClicker : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        public UnityEvent onClick = new UnityEvent();
        
        public void OnPointerClick(PointerEventData eventData)
        {
            onClick.Invoke();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            onClick.Invoke();
        }
    }
}