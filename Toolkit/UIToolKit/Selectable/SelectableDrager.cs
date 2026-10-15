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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            onDrag?.RemoveAllListeners();
            onDrag = null;
            onBeginDrag?.RemoveAllListeners();
            onBeginDrag = null;
            onEndDrag?.RemoveAllListeners();
            onEndDrag = null;
        }
        
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