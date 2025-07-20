using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class DragPass : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public GameObject target;
        public bool horizontal = true;
        public bool vertical = true;

        private bool _canPass;

        private bool CanPass(PointerEventData eventData)
        {
            if (horizontal && vertical) return true;
            if (horizontal)
            {
                var dir = new Vector2(Mathf.Abs(eventData.delta.x), Mathf.Abs(eventData.delta.y));
                return Vector2.Angle(Vector2.right, dir) <= 45f;
            }

            if (vertical)
            {
                var dir = new Vector2(Mathf.Abs(eventData.delta.x), Mathf.Abs(eventData.delta.y));
                return Vector2.Angle(Vector2.up, dir) < 45f;
            }
            
            // var results = new List<RaycastResult>();
            // EventSystem.current.RaycastAll(eventData, results);
            // var current = eventData.pointerCurrentRaycast.gameObject;
            // for (int i = 0; i < results.Count; i++)
            // {
            //     var go = results[i].gameObject;
            //     if(go == current) continue;
            //     if (target && go == target)
            //     {
            //         return true;
            //     }
            // }
            return false;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if(!_canPass) return;
            PassEvent(eventData, ExecuteEvents.dragHandler);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _canPass = CanPass(eventData);
            if(!_canPass) return;
            PassEvent(eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if(!_canPass) return;
            PassEvent(eventData, ExecuteEvents.endDragHandler);
        }

        private void PassEvent<T>(PointerEventData data, ExecuteEvents.EventFunction<T> function) 
            where T :IEventSystemHandler
        {
            if(!_canPass) return;
            ExecuteEvents.Execute(target, data, function);
        }
    }
}