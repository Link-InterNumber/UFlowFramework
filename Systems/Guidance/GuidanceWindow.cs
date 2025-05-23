using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [WindowInfo("Assets/Test/GuidanceTest/GuidanceWindow.prefab")]
    public class GuidanceWindow : UIWindow, IUIStandAlone
    {
        public Graphic graphics;
        public Button screenButton;
        private GameObject _uiPrefab;
        private GuidanceTag _guidanceTag;
        private bool _canSkip;
        
        public struct Info
        {
            public GuidanceTag tag;
            public GuidanceConf conf;
        }

        public override void OnFocus()
        {
            
        }

        public override void RegisterEvent()
        {
            screenButton.onClick.AddListener(SkipGuidance);
        }

        public override void DeregisterEvent()
        {
            screenButton.onClick.RemoveListener(SkipGuidance);
        }

        public override void OnOpen(object data)
        {
            if (graphics) graphics.raycastTarget = true;
            var guidanceInfo = (Info) data;
            _guidanceTag = guidanceInfo.tag;
            screenButton.gameObject.SetActive(guidanceInfo.conf.touchScreenToSkip ||
                                              guidanceInfo.conf.blockInteraction ||
                                              !_guidanceTag.GetComponent<RectTransform>());
            screenButton.GetComponent<Canvas>().sortingOrder = guidanceInfo.conf.blockInteraction ? 6000 : 4000;
            if (_uiPrefab)
            {
                GameObject.Destroy(_uiPrefab);
                _uiPrefab = null;
            }

            if (!guidanceInfo.conf.uiPrefab.isNull)
            {
                _canSkip = false;
                assetsLoader.AsyncLoadNInstantiate(guidanceInfo.conf.uiPrefab.assetName, OnLoadUiPrefab);
            }
            else
            {
                _canSkip = true;
            }
        }

        private void SkipGuidance()
        {
            if(!_canSkip) return;
            GuidanceManager.instance.DeExecuteGuidance(_guidanceTag.guidanceIndex);
        }

        private void OnLoadUiPrefab(GameObject obj)
        {
            _canSkip = true;
            if (_uiPrefab)
            {
                GameObject.Destroy(_uiPrefab);
            }
            _uiPrefab = obj;
            var hand = obj.GetComponent<GuidanceHand>();
            if (hand)
            {
                hand.Init(_guidanceTag);
            }
            _uiPrefab.transform.SetParent(transform);
            _uiPrefab.transform.localScale = Vector3.one;
        }

        public override void OnClose()
        {
            if (_uiPrefab)
            {
                GameObject.Destroy(_uiPrefab);
                _uiPrefab = null;
            }
        }
    }
}