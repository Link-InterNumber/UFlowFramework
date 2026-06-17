using System;
using UnityEngine;

namespace PowerCellStudio
{
    public abstract class OpenWindowRequestBase : IOpenWindowRequest
    {
        protected Type _windowType;
        protected object _data;
        protected Action _beforeOpen;

        protected bool _isPreLoad;
        public bool isPreLoad => _isPreLoad;
        
        protected bool _raycastTarget;
        protected IUIParent _parent;
        
        protected AssetLoadStatus _assetLoadStatus;
        protected bool _ignoreRaycaster;
        protected bool _standaloneCanvas;
        protected string _windowPath;
        public AssetLoadStatus assetLoadStatus => _assetLoadStatus;
        public Type currentWindowType => _windowType;

        protected Action _onLoaded;
        
        public void OnLoaded(Action onLoaded)
        {
            _onLoaded = onLoaded;
        }

        public OpenWindowRequestBase(IUIParent parent, Type windowType, bool preload, object sourceData, Action beforeOpen)
        {
            _parent = parent;
            _windowType = windowType;
            _isPreLoad = preload;
            _assetLoadStatus = AssetLoadStatus.Unload;
            _data = sourceData;
            _beforeOpen = beforeOpen;
        }
        
        public void SetOpenData(object sourceData, Action beforeOpen)
        {
            _data = sourceData;
            _beforeOpen = beforeOpen;
            _isPreLoad = false;
        }

        public void Load()
        {
            if (_assetLoadStatus != AssetLoadStatus.Unload || !_parent.transform) return;
            GetWindowInfo(_windowType, out _windowPath, out _ignoreRaycaster, out _standaloneCanvas);
            _assetLoadStatus = AssetLoadStatus.Loading;
            if (_windowPath == null)
            {
                UILogger.LogError($"{_windowType.Name}没有配置预制体路径");
                _assetLoadStatus = AssetLoadStatus.Loaded;
                return;
            }
            _parent.assetsLoader.AsyncLoadNInstantiate(_windowPath, OnLoadSuccess, OnLoadFailed);
        }

        protected abstract void GetWindowInfo(Type windowType, out string path, out bool ignoreRaycast, out bool standaloneCanvas);

        private void OnLoadSuccess(GameObject go)
        {
            _assetLoadStatus = AssetLoadStatus.Loaded;
            // go.SetActive(false);
            var ui = GetWindowInstance(_windowType, go);
            if (ui == null)
            {
                UILogger.LogError($"{_windowType.Name}实例化失败");
                ApplicationManager.instance.DelayedNextFrame(() =>
                {
                    GameObject.Destroy(go);
                });
                _onLoaded?.Invoke();
                _onLoaded = null;
                return;
            }
            ui.prefabPath = _windowPath;
            if (!_parent.transform || _parent.windowRequests == null)
            {
                UILogger.LogError($"正在打开【{_windowType.Name}】但【{_parent.GetType().Name}】已经被销毁");
                ApplicationManager.instance.DelayedNextFrame(() =>
                {
                    GameObject.Destroy(go);
                });
                _onLoaded?.Invoke();
                _onLoaded = null;
                return;
            }
            UIUtils.SetUIChildToParent(ui, _parent);
            UIUtils.InitUI(ui, _ignoreRaycaster, _standaloneCanvas, UIManager.instance.canvasRenderMode);
            if (_isPreLoad)
            {
                ui.transform.gameObject.SetActive(false);
                _onLoaded?.Invoke();
                _onLoaded = null;
                return;
            }
            _beforeOpen?.Invoke();
            UIUtils.OpenUI(ui, _data);
            _onLoaded?.Invoke();
            _onLoaded = null;
        }

        protected abstract IUIChild GetWindowInstance(Type windowType, GameObject instanceWindow);
        
        private void OnLoadFailed()
        {
            _assetLoadStatus = AssetLoadStatus.Loaded;
            _onLoaded?.Invoke();
            _onLoaded = null;
        }
    }
}