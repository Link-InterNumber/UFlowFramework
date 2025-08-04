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

        private enum State
        {
            Opened,
            WaitToClose,
            CanClose,
            Closed,
        }
        private State _state;

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
            _state = State.Opened;
            if (graphics) graphics.raycastTarget = true;
            var guidanceInfo = (Info) data;
            _guidanceTag = guidanceInfo.tag;
            _canSkip = guidanceInfo.conf.touchScreenToSkip || !_guidanceTag.GetComponent<RectTransform>();
            screenButton.gameObject.SetActive(_canSkip || guidanceInfo.conf.blockInteraction);
            screenButton.GetComponent<Canvas>().sortingOrder = guidanceInfo.conf.blockInteraction ? 6000 : 4000;
            if (_uiPrefab)
            {
                GameObject.Destroy(_uiPrefab);
                _uiPrefab = null;
            }
            if (!guidanceInfo.conf.uiPrefab.isNull)
            {
                assetsLoader.AsyncLoadNInstantiate(guidanceInfo.conf.uiPrefab.assetName, OnLoadUiPrefab);
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
            _state = State.Closed;
            if (!_uiPrefab) return;
            GameObject.Destroy(_uiPrefab);
            _uiPrefab = null;
        }

        bool IUIComponent.Close()
        {
            switch (_state)
            {
                case State.Opened:
                    _state = State.WaitToClose;
                    ApplicationManager.RunCoroutine(WaitToClose(0.5f));
                    return false;
                case State.WaitToClose:
                    return false;
                case State.CanClose:
                    return true;
                case State.Closed:
                    return false;
                default:
                    return true;
            }
            return true;
        }

        private IEnumerator WaitToClose(float waitTime)
        {
            var time = 0f;
            while (time < waitTime)
            {
                if (_state == State.Opened) yield break;
                time += Time.unscaledDeltaTime;
            }
            _state = State.CanClose;
            CloseUI(null);
        }
        
    }
}