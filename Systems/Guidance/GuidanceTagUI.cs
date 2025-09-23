using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
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
                gameObject.AddComponent<GraphicRaycaster>();
                // 一些Unity版本中，Canvas在创建时会优先使用默认配置，因此延迟一帧注册
                ApplicationManager.instance.DelayedNextFrame(()=>{
                    _tempCanvas.overrideSorting = true;
                    _tempCanvas.sortingLayerID = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
                    _tempCanvas.sortingOrder = 5500;
                });
            }

            var selsctable = GetComponent<Selectable>();
            if (selsctable)
            {
                selsctable.interactable = true;
                selsctable.Select();
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