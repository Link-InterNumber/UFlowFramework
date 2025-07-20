using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class SelectableDrager : SelectableInteractor, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public UnityEvent<PointerEventData> onDrag = new UnityEvent<PointerEventData> ();
        public UnityEvent<PointerEventData> onBeginDrag = new UnityEvent<PointerEventData> ();
        public UnityEvent<PointerEventData> onEndDrag = new UnityEvent<PointerEventData> ();
        
        public void OnDrag(PointerEventData eventData)
        {
            onDrag.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            onBeginDrag.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            onEndDrag.Invoke(eventData);
        }
    }
}