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
            debugBtnGameObject.AddComponent<DebugBtn>();
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