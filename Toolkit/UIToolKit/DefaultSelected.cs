using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    [DisallowMultipleComponent]
    public class DefaultSelected : SelectableInteractor, ISelectHandler
    {
        public UnityEvent onSelected = new UnityEvent();
        
        private void OnEnable()
        {
            if(target) target.Select();
        }

        public void OnSelect(BaseEventData eventData)
        {
            onSelected.Invoke();
        }
    }
}