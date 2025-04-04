using System;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public class PoolWindowPage : UIPage
    {
        public override void OnOpen(object data)
        {
            
        }

        public void OpenUI<T>(IUIParent page, object data, Action beforeOpen) where T : UIBehaviour, IUIChild
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
            // 限制每次只能打开一个界面
            // if (_windowRequests.Values.Any(o => !o.isPreLoad))
            // {
            //     UILog.LogWarning($"{GetType().Name}有一个窗口正在打开");
            //     return;
            // }
            var window = GetUI<T>();
            if (window != null)
            {
                UIUtils.SetUIChildToParent(window, page);
                UIUtils.OpenUI(window, data);
                beforeOpen?.Invoke();
                return;
            }
            var newRequest = new OpenWindowRequest(this, panelType, false, data, () =>
            {
                var openedWindow = GetUI<T>();
                UIUtils.SetUIChildToParent(openedWindow, page);
                beforeOpen?.Invoke();
            });
            newRequest.Load();
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