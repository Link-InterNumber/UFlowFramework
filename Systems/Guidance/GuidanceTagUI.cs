using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class GuidanceTagUI : GuidanceTag, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        private Canvas _tempCanvas;
        private Graphic _graphic;
        
        public override void OnExecute()
        {
            if(_inExecute) return;
            _inExecute = true;
            var canvas = GetComponent<Canvas>();
            if (!canvas)
            {
                _tempCanvas = gameObject.AddComponent<Canvas>();
                _tempCanvas.renderMode = UIManager.instance.canvasRenderMode;
                _tempCanvas.overrideSorting = true;
                _tempCanvas.sortingLayerName = "UI";
                _tempCanvas.sortingOrder = 5000;
                if (UIManager.instance.canvasRenderMode != RenderMode.ScreenSpaceOverlay)
                    _tempCanvas.worldCamera = UICamera.instance.cameraCom;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var selsctable = GetComponent<Selectable>();
            if (selsctable)
            {
                selsctable.interactable = true;
            }
            else
            {
                var graphic = GetComponent<Graphic>();
                if (graphic)
                {
                    graphic.raycastTarget = true;
                }
                else
                {
                    _graphic = gameObject.AddComponent<EmptyRaycast>();
                    _graphic.raycastTarget = true;
                }
            }
        }

        public override void OnDeExecute()
        {
            if(!_inExecute) return;
            _inExecute = false;
            if (_graphic)
            {
                Destroy(_graphic);
            }
            if(_tempCanvas)
            {
                var GR = gameObject.GetComponent<GraphicRaycaster>();
                if(GR) Destroy(GR);
                Destroy(_tempCanvas);
            }
        }

        public override Vector2 GetUIPosition()
        {
            return transform.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(!_inExecute) return;
            GuidanceManager.instance.DeExecuteGuidance(guidanceIndex);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if(!_inExecute) return;
            GuidanceManager.instance.DeExecuteGuidance(guidanceIndex);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(!_inExecute) return;
        }
    }
}