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

        void IUIComponent.Open(object data)
        {
            OnOpen(data);
        }

        bool IUIComponent.Close()
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
        public bool isOpened => window?.isOpened ?? false;

        public virtual void RegisterEvent()
        {
            if (window.closeBtn == null) return;
            foreach (var button in window.closeBtn)
            {
                if (!button) continue;
                button.onClick.AddListener(OnCloseBtnClick);
            }
        }

        public virtual void DeregisterEvent()
        {
            if (window.closeBtn == null) return;
            foreach (var button in window.closeBtn)
            {
                if (!button) continue;
                button.onClick.RemoveListener(OnCloseBtnClick);
            }
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

        public abstract void OnFocus();

        public virtual void OnHide(){}

        public abstract void OnClose();

        public virtual void OnUIDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetsLoader);
            _assetsLoader = null;
        }
    }
}