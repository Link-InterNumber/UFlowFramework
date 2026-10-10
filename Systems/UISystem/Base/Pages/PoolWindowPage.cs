using System;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class PoolWindowPage : UIPage
    {
        public override void OnOpen(object data)
        {
            if(_assetsLoader == null) _assetsLoader = AssetUtils.SpawnLoader(gameObject.name);
        }

        public void OpenUI<T>(IUIParent page, object data, Action beforeOpen) where T : class, IUIChild
        {
            var panelType = typeof(T);
            if (IsUIGoingToOpen<T>(out var request))
            {
                request.SetOpenData(data, () =>
                {
                    var openedWindow = GetUI<T>();
                    UIUtils.SetUIChildToParent(openedWindow, page);
                    beforeOpen?.Invoke();
                });
                return;
            }
            var window = GetUI<T>();
            if (window != null)
            {
                UIUtils.SetUIChildToParent(window, page);
                UIUtils.OpenUI(window, data);
                beforeOpen?.Invoke();
                return;
            }
            var newRequest = OpenWindowRequestFactroy.Create(this, panelType, false, data, () =>
            {
                var openedWindow = GetUI<T>();
                UIUtils.SetUIChildToParent(openedWindow, page);
                beforeOpen?.Invoke();
            });
            _windowRequests.AddWindowRequest(newRequest);
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