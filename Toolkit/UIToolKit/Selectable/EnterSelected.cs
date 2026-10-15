using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class EnterSelected : SelectableInteractor, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private bool _cachedSelected = false;

        public bool restoreSelectedState;

        public UnityEvent<bool> onSelected = new UnityEvent<bool>();
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            onSelected?.RemoveAllListeners();
            onSelected = null;
        }

        private void OnEnable()
        {
            if (target && target.gameObject == EventSystem.current.currentSelectedGameObject)
                onSelected?.Invoke(true);
            else
                onSelected?.Invoke(false);
            
            if (!restoreSelectedState || !_cachedSelected) return;
            target?.Select();
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            onSelected?.Invoke(true);
            _cachedSelected = true;
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            onSelected?.Invoke(false);
            _cachedSelected = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            target?.Select();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // do nothing;
        }
    }
}