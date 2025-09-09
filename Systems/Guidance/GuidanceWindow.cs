using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PowerCellStudio
{
    [WindowInfo("Assets/Res/UI/Common/GuidanceWindow.prefab")]
    public class GuidanceWindow : UIWindow, IUIStandAlone, IUIComponent
    {
        public Graphic graphics;
        public Button screenButton;

        private GameObject _uiPrefab;
        private GuidanceTag _guidanceTag;
        private IGuidanceConfig _conf;
        private bool _canSkip;
        private string _currentPrefab;
        
        public struct Info
        {
            public GuidanceTag tag;
            public IGuidanceConfig conf;
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
            base.RegisterEvent();
            screenButton.onClick.AddListener(SkipGuidance);
        }

        public override void DeregisterEvent()
        {
            base.DeregisterEvent();
            screenButton.onClick.RemoveListener(SkipGuidance);
        }
        
        public override void OnOpen(object data)
        {
            _state = State.Opened;
            if (graphics) graphics.raycastTarget = true;
            var guidanceInfo = (Info) data;
            _guidanceTag = guidanceInfo.tag;
            _conf = guidanceInfo.conf;
            _canSkip = guidanceInfo.conf.touchScreenToSkip ||guidanceInfo.conf.blockInteraction || !_guidanceTag.GetComponent<RectTransform>();
            screenButton.gameObject.SetActive(_canSkip || guidanceInfo.conf.blockInteraction);
            screenButton.GetComponent<Canvas>().sortingOrder = guidanceInfo.conf.blockInteraction ? 6000 : 4000;
            if (!string.IsNullOrEmpty(_currentPrefab) && _currentPrefab.Equals(guidanceInfo.conf.uiPrefab.assetName))
            {
                var hand = _uiPrefab.GetComponent<GuidanceHand>();
                if (!hand) return;
                var currentConfig =
                    ConfigManager.instance.guidanceConf.Get(GuidanceManager.instance.currentIndex.Last());
                hand.Init(_guidanceTag, currentConfig?.decs.Get());
                return;
            }
            
            if (_uiPrefab)
            {
                GameObject.Destroy(_uiPrefab);
                _uiPrefab = null;
            }
            if (!guidanceInfo.conf.uiPrefab.isNull)
            {
                assetsLoader.LoadAsync<GameObject>(guidanceInfo.conf.uiPrefab.assetName, OnLoadUiPrefab);
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
            _uiPrefab = GameObject.Instantiate(obj);
            var hand = _uiPrefab.GetComponent<GuidanceHand>();
            if (hand)
            {
                hand.Init(_guidanceTag, _conf?.decs.Get());
                hand.transform.position = _guidanceTag.GetUIPosition();
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