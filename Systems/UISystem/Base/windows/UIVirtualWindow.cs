using System;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class UIVirtualWindow<T> : IUIChild
        where T : UIWindow
    {
        protected T window;
        private IUIParent _parent;
        IUIParent IUIChild.parent { get => _parent; set => _parent = value; }
        private string _prefabPath;
        string IUIChild.prefabPath { get => _prefabPath; set => _prefabPath = value; }

        public UIVirtualWindow(){}

        public void BindWindow(UIWindow window)
        {
            this.window = window as T;
        }
        
        void IUIComponent.OnUIInstanced()
        {
            _assetsLoader = AssetUtils.SpawnLoader(this.GetType().Name);
            OnInstanced();
        }

        /// <summary>
        /// 在UI实例化后调用 /
        /// Called when the UI is instantiated, override this method to perform additional initialization.
        /// </summary>
        protected virtual void OnInstanced() { }
        
        void IUIComponent.OnUIDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetsLoader);
            _assetsLoader = null;
            OnDestroy();
        }

        /// <summary>
        /// 在UI销毁时执行 / Executed on UI destruction
        /// </summary>
        protected virtual void OnDestroy(){}

        void IUIComponent.Open(object data)
        {
            _isOpened = true;
            OnOpen(data);
        }

        bool IUIComponent.Close()
        {
            _isOpened = !CheckCloseCondition();
            return !_isOpened;
        }
        
        protected virtual bool CheckCloseCondition()
        {
            return true;
        }

        private IAssetLoader _assetsLoader;
        public IAssetLoader assetsLoader
        {
            get
            {
                if (_assetsLoader == null || !_assetsLoader.spawned)
                    _assetsLoader = AssetUtils.SpawnLoader(this.GetType().Name);
                return _assetsLoader;
            }
        }
        public Transform transform => window?.transform ?? null;
        public RectTransform rectTransform => window?.rectTransform ?? null;
        private bool _isOpened;
        public bool isOpened => _isOpened;
        
        protected UIEventHost _eventHost;

        public void RegisterEvent()
        {
            _eventHost = UIEventHostPool.Get();
            RegisterEvent(_eventHost);
            if (window.closeBtn == null) return;
            foreach (var button in window.closeBtn)
            {
                if (!button) continue;
                _eventHost.AddListener(button, OnCloseBtnClick);
            }
        }
        
        protected virtual void RegisterEvent(UIEventHost eventHost)
        {
            
        }

        public void DeregisterEvent()
        {
            DeregisterEvent(_eventHost);
            UIEventHostPool.Release(_eventHost);
            _eventHost = null;
            // if (window.closeBtn == null) return;
            // foreach (var button in window.closeBtn)
            // {
            //     if (!button) continue;
            //     button.onClick.RemoveListener(OnCloseBtnClick);
            // }
        }
        
        protected virtual void DeregisterEvent(UIEventHost eventHost)
        {
            
        }

        protected virtual void OnCloseBtnClick()
        {
            CloseUI(null);
        }

        protected virtual void CloseUI(Action afterClosed)
        {
            _parent.CloseUI(this, afterClosed);
        }

        public abstract void OnOpen(object data);
        
        public abstract void OnClose();

        public abstract void OnFocus();

        public virtual void OnHide(){}
    }
}