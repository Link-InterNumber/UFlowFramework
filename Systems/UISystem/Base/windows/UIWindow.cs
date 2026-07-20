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

        public IAssetLoader assetsLoader
        {
            get
            {
                if (_assetsLoader == null || !_assetsLoader.spawned)
                    _assetsLoader = AssetUtils.SpawnLoader(this.GetType().Name);
                return _assetsLoader;
            }
        }

        private bool _isOpened;
        public bool isOpened => _isOpened;

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
            var root = adaptiveRoot;
            if (!root) return;

            var safeArea = Screen.safeArea;
            var scale = UIManager.PixelScale;
            var offsetMin = new Vector2(
                Mathf.Max(0, safeArea.min.x * scale),
                Mathf.Max(0, safeArea.min.y * scale));
            var offsetMax = safeArea.max * scale - UIManager.ScreenSize;
            offsetMax.x = Mathf.Min(0, offsetMax.x);
            offsetMax.y = Mathf.Min(0, offsetMax.y);

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = offsetMin;
            root.offsetMax = offsetMax;
        }

        public RectTransform rectTransform => transform as RectTransform;
        
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

        public abstract void OnOpen(object data);
        
        public abstract void OnClose();
        
        public virtual void OnFocus(){}
        
        public virtual void OnHide(){}
        
        public virtual void OnUIDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetsLoader);
            _assetsLoader = null;
        }

        protected UIEventHost _eventHost;
        
        public void RegisterEvent()
        {
            OnCanvasHierarchyChanged();
            var eventHost = UIEventHostPool.Get();
            _eventHost = eventHost;

            RegisterEvent(eventHost);

            var buttons = closeBtn;
            if (buttons == null) return;

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (!button) continue;
                eventHost.AddListener(button, OnCloseBtnClick);
            }
        }

        protected virtual void RegisterEvent(UIEventHost eventHost)
        {
            
        }

        public void DeregisterEvent()
        {
            var eventHost = _eventHost;
            DeregisterEvent(eventHost);
            UIEventHostPool.Release(eventHost);
            _eventHost = null;
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

        #region UI Operations

        protected void SetRaycastEnable(bool enable)
        {
            var canvasGroup = gameObject.TryAddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = enable;
        }
        
        protected LoaderYieldInstruction<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            return _assetsLoader.LoadAsYieldInstruction<T>(path);
        }

        #endregion
    }
}