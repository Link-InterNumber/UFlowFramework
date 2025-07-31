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

        private RectTransform _rectTransform;
        protected RectTransform rectTransform
        {
            get
            {
                if (!_rectTransform) _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }
        
        private float _outSpaceTime;

        public override void OnExecute()
        {
            if(_inExecute) return;
            _outSpaceTime = 0f;
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
            selsctable.Select();
        }

        public override void OnDeExecute()
        {
            if(!_inExecute) return;
            _inExecute = false;
            if (_graphic)
            {
                Destroy(_graphic);
                _graphic = null;
            }
            if(_tempCanvas)
            {
                var GR = gameObject.GetComponent<GraphicRaycaster>();
                if(GR) Destroy(GR);
                Destroy(_tempCanvas);
                _tempCanvas = null;
            }
        }

        public override Vector2 GetUIPosition()
        {
            return transform.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if(!_inExecute) return;
            GuidanceManager.instance.DeExecuteGuidance(guidanceIndex);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(!_inExecute) return;
            GuidanceManager.instance.DeExecuteGuidance(guidanceIndex);
        }

        public void Update()
        {
            if (!_inExecute) return;
            if (rectTransform == null) return;

            var isMaxOut = UIManager.IsRectOutOfScreen(rectTransform);
            if (!isMaxOut) return;
            _outSpaceTime += Time.unscaledDeltaTime;
            if (!(_outSpaceTime > 10f)) return;
            GuidanceManager.instance.DeExecuteGuidance(guidanceIndex);
            OnDeExecute();
        }
    }
}