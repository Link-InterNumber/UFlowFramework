using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerCellStudio
{
    public abstract class UIWindow : UIBehaviour, IUIChild
    {
        #region button
        [Header("Adaptive Root")]
        public RectTransform adaptiveRoot;
        [Header("Default Button"), SpaceAfter(10)]
        public Button[] closeBtn;

        #endregion
        
        private IAssetLoader _assetsLoader;
        private IUIParent _parent;
        // private Canvas _canvas;

        public IAssetLoader assetsLoader => _assetsLoader;

        public bool isOpened => gameObject.activeSelf;
        public virtual void OnUIDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetsLoader);
            _assetsLoader = null;
        }

        IUIParent IUIChild.parent
        {
            get => _parent;
            set => _parent = value;
        }

        // Canvas IUIChild.canvas
        // {
        //     get => _canvas;
        //     set => _canvas = value;
        // }
        
        private string _prefabPath;
        string IUIChild.prefabPath { get => _prefabPath; set => _prefabPath = value; }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            // 获取屏幕安全区
            if (!adaptiveRoot) return;
            var safeArea = Screen.safeArea;
            var scale = UIManager.PixelScale;
            adaptiveRoot.anchorMin = Vector2.zero;
            adaptiveRoot.anchorMax = Vector2.one;
            adaptiveRoot.offsetMin = safeArea.min * scale;
            adaptiveRoot.offsetMax = safeArea.max * scale - UIManager.ScreenSize;
        }

        public RectTransform rectTransform => transform as RectTransform;
        void IUIComponent.Open(object data)
        {
            if(_assetsLoader == null || !_assetsLoader.spawned)
                _assetsLoader = AssetUtils.SpawnLoader(this.GetType().Name);
            OnOpen(data);
        }

        public abstract void OnOpen(object data);

        bool IUIComponent.Close()
        {
            OnClose();
            return true;
        }

        public abstract void OnClose();
        
        public abstract void OnFocus();

        public virtual void RegisterEvent()
        {
            if (closeBtn == null) return;
            foreach (var button in closeBtn)
            {
                if (!button) continue;
                button.onClick.AddListener(OnCloseBtnClick);
            }
        }

        public virtual void DeregisterEvent()
        {
            if (closeBtn == null) return;
            foreach (var button in closeBtn)
            {
                if (!button) continue;
                button.onClick.RemoveListener(OnCloseBtnClick);
            }
        }

        protected LoaderYieldInstruction<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            return _assetsLoader.LoadAsYieldInstruction<T>(path);
        }

        protected virtual void OnCloseBtnClick()
        {
            CloseUI(null);
        }

        protected void CloseUI(Action onClosed)
        {
            _parent.CloseUI(this, onClosed);
        }
    }
}