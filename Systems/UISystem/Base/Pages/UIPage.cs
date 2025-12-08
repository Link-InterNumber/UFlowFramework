using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public abstract class UIPage : UIBehaviour, IUIParent
    {
        protected Canvas _canvasCom;
        public Canvas canvasCom {get => _canvasCom; set => _canvasCom = value;}

        protected PagePushMode _pushMode;
        public PagePushMode pushMode {get => _pushMode; set => _pushMode = value;}

        protected IAssetLoader _assetsLoader;
        public IAssetLoader assetsLoader => _assetsLoader;
        
        protected HashStack<IUIChild> _openedUIs = new HashStack<IUIChild>();
        HashStack<IUIChild> IUIParent.openedUIs => _openedUIs;

        protected Dictionary<Type, IUIChild> _children = new Dictionary<Type, IUIChild>();
        Dictionary<Type, IUIChild> IUIParent.children => _children;

        protected OpenWindowRequestHolder _windowRequests = new OpenWindowRequestHolder();
        OpenWindowRequestHolder IUIParent.windowRequests
        {
            get => _windowRequests;
            set => _windowRequests = value;
        }
        
        public RectTransform rectTransform => transform as RectTransform;

        public bool isOpened => gameObject.activeSelf;
        
        public void OnUIDestroy()
        {
            AssetUtils.DeSpawnLoader(_assetsLoader);
            _assetsLoader = null;
            _children = null;
            _openedUIs = null;
            _windowRequests = null;
        }

        public void OpenUI<T>(object data, Action beforeOpen = null) where T : class, IUIChild
        {
            var panelType = typeof(T);
            if (IsUIGoingToOpen<T>(out var request))
            {
                request.SetOpenData(data, beforeOpen);
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
                beforeOpen?.Invoke();
                UIUtils.OpenUI(window, data);
                return;
            }
            var newRequest = OpenWindowRequestFactroy.Create(this, panelType, false, data, beforeOpen);
            _windowRequests.AddWindowRequest(newRequest);
        }

        public void PreloadUI<T>() where T : class, IUIChild
        {
            if (IsUIGoingToOpen<T>(out _)) return;
            var window = GetUI<T>();
            if (window != null) return;
            var newRequest = OpenWindowRequestFactroy.Create(this, typeof(T), true, null, null);
            _windowRequests.AddWindowRequest(newRequest);
        }

        public bool CloseUI<T>(Action onClosed) where T : class, IUIChild
        {
            var window = GetUI<T>();
            if (window == null) return false;
            if (!window.isOpened) return true;
            var isPeek = GetTopUI().Equals(window);
            if (!UIUtils.CloseUI<T>(window, onClosed)) return false;
            if (isPeek)
            {
                GetTopUI()?.OnFocus();
            }
            return true;
        }

        bool IUIParent.CloseUI<T>(T uiChild, Action afterClosed)
        {
            if(uiChild == null || !_openedUIs.Contains(uiChild)) return false;
            var isPeek = GetTopUI().Equals(uiChild);
            if (!UIUtils.CloseUI<T>(uiChild, afterClosed)) return false;
            if(isPeek)
            {
                GetTopUI()?.OnFocus();
            }
            return true;
        }

        public T GetOpenedUI<T>() where T : class, IUIChild
        {
            return _openedUIs?.LastOrDefault(x => x is T) as T;
        }

        public bool IsWindowOpened<T>() where T : class, IUIChild
        {
            return GetOpenedUI<T>() != null;
        }

        public T GetUI<T>() where T : class, IUIChild
        {
            return _children.TryGetValue(typeof(T), out var child) ? (T) child : null;
        }

        public bool IsUIGoingToOpen<T>(out IOpenWindowRequest request) where T : class, IUIChild
        {
            return _windowRequests.IsUIGoingToOpen<T>(out request);
        }

        public IUIChild GetTopUI()
        {
            return (_openedUIs != null && _openedUIs.Count > 0) ? _openedUIs.Peek() : null;
        }

        void IUIComponent.Open(object data)
        {
            if(_assetsLoader == null) _assetsLoader = AssetUtils.SpawnLoader(gameObject.name);
            OnOpen(data);
        }

        public abstract void OnOpen(object data);

        bool IUIComponent.Close()
        {
            OnClose();
            return true;
        }

        public abstract void OnClose();

        public virtual void OnFocus()
        {
        }

        public abstract void RegisterEvent();

        public abstract void DeregisterEvent();
        
        protected LoaderYieldInstruction<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            return _assetsLoader.LoadAsYieldInstruction<T>(path);
        }
    }
}