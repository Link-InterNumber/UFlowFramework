using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public class StandAlonePage : UIPage
    {
        public static StandAlonePage Create(Transform parent, RenderMode canvasRenderMode)
        {
            var go = new GameObject("StandAlonePage");
            var standAlonePage = go.AddComponent<StandAlonePage>();
            go.AddComponent<RectTransform>();
            standAlonePage.transform.SetParent(parent);
            standAlonePage.transform.localScale = Vector3.one;
            standAlonePage.transform.localPosition = Vector2.zero;
            UIUtils.InitCanvas(standAlonePage, true, canvasRenderMode);
            standAlonePage.transform.GetComponent<Canvas>().sortingLayerID = SortingLayer.NameToID("UI");
            standAlonePage.RegisterEvent();
            standAlonePage.OnOpen(null);
            standAlonePage.OnFocus();
            // standAlonePage.PreloadUI<MaskWindow>();
#if UNITY_EDITOR || DEBUG
            var debugBtnGameObject = new GameObject("DebugBtn");
            var btnRect = debugBtnGameObject.AddComponent<RectTransform>();
            btnRect.SetParent(standAlonePage.transform);
            btnRect.localScale = Vector3.one;
            var size = Mathf.Min(UIManager.ScreenSize.x, UIManager.ScreenSize.y) * 0.1f; 
            btnRect.sizeDelta = new Vector2(size, size);
            btnRect.anchorMin = Vector2.one;
            btnRect.anchorMax = Vector2.one;
            
            var safeArea = Screen.safeArea;
            var scale = UIManager.PixelScale;
            btnRect.anchoredPosition = safeArea.max * scale - UIManager.ScreenSize - new Vector2(size/2, size/2);
            
            var btnColor = Color.yellow;
            btnColor.a = 0.5f;
            debugBtnGameObject.AddComponent<Image>().color = btnColor;
            var debugBtn = debugBtnGameObject.AddComponent<Button>();
            debugBtn.onClick.AddListener(() =>
            {
                UIManager.instance.OpenWindow<DebugWindow>();
            });
#endif
            return standAlonePage;
        }
        
        public override void OnOpen(object data)
        {
            if(_assetsAssetLoader == null) _assetsAssetLoader = AssetUtils.SpawnLoader(gameObject.name);
        }

        public override void OnClose()
        {
            
        }

        public override void RegisterEvent()
        {
            
        }

        public override void DeregisterEvent()
        {
            
        }
    }
}