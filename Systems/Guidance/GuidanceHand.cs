using UnityEngine;

namespace PowerCellStudio
{
    public class GuidanceHand : MonoBehaviour
    {
        private GuidanceTag _guidanceTag;

        public void Init(GuidanceTag tag)
        {
            _guidanceTag = tag;
        }

        private void Start()
        {
            var canvas = transform.GetComponent<Canvas>();
            if (!canvas)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = UIManager.instance.canvasRenderMode;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 6000;
            if (UIManager.instance.canvasRenderMode != RenderMode.ScreenSpaceOverlay)
                canvas.worldCamera = UICamera.instance.cameraCom;
        }

        private void Update()
        {
            if(!_guidanceTag) return;
            transform.position = _guidanceTag.GetUIPosition();
        }
    }
}