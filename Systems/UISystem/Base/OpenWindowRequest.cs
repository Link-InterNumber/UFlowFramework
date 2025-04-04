using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PowerCellStudio
{
    public interface IOpenWindowRequest
    {
        Type currentWindowType { get; }
        bool isPreLoad { get; }
        AssetLoadStatus assetLoadStatus { get; }
        void Load();
        void SetOpenData(object sourceData, Action beforeOpen);
        void OnLoaded(Action onLoaded);
    }

    public class OpenWindowRequestHolder
    {
        private List<IOpenWindowRequest> _windowRequests = new List<IOpenWindowRequest>();
        
        public int Count => _windowRequests.Count;
        
        public void AddWindowRequest(IOpenWindowRequest request)
        {
            _windowRequests.Add(request);
            TriggerRequest();
        }
        
        private void TriggerRequest()
        {
            if (_windowRequests.Count == 0) return;
            while (_windowRequests.Count > 0)
            {
                var request = _windowRequests[0];
                if (request.assetLoadStatus == AssetLoadStatus.Loaded)
                {
                    _windowRequests.RemoveAt(0);
                    continue;
                }
                if (request.assetLoadStatus == AssetLoadStatus.Loading)
                {
                    break;
                }
                request.OnLoaded(TriggerRequest);
                request.Load();
                break;
            }
        }
        
        public bool IsUIGoingToOpen(Type windowType, out IOpenWindowRequest request)
        {
            request = null;
            if (_windowRequests == null || _windowRequests.Count == 0)
            {
                return false;
            }
            for (var i = 0; i < _windowRequests.Count; i++)
            {
                var req = _windowRequests[i];
                if (req.currentWindowType == windowType)
                {
                    request = req;
                    return true;
                }
            }
            return false;
        }
        
        public bool IsUIGoingToOpen<T>(out IOpenWindowRequest request) where T : UIBehaviour, IUIChild
        {
            request = null;
            if (_windowRequests == null || _windowRequests.Count == 0)
            {
                return false;
            }
            var type = typeof(T);
            for (var i = 0; i < _windowRequests.Count; i++)
            {
                var req = _windowRequests[i];
                if (req.currentWindowType == type)
                {
                    request = req;
                    return true;
                }
            }
            return false;
        }
    }
    
    public class OpenWindowRequest : IOpenWindowRequest
    {
        private Type _windowType;
        private object _data;
        private Action _beforeOpen;

        private bool _isPreLoad;
        public bool isPreLoad => _isPreLoad;
        
        private bool _raycastTarget;
        private IUIParent _parent;
        
        private AssetLoadStatus _assetLoadStatus;
        private bool _ignoreRaycaster;
        private string _windowPath;
        public AssetLoadStatus assetLoadStatus => _assetLoadStatus;
        public Type currentWindowType => _windowType;

        private Action _onLoaded;
        
        public void OnLoaded(Action onLoaded)
        {
            _onLoaded = onLoaded;
        }

        public OpenWindowRequest(IUIParent parent, Type windowType, bool preload, object sourceData, Action beforeOpen)
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
            if (_assetLoadStatus != AssetLoadStatus.Unload) return;
            _windowPath = null;
            
            _ignoreRaycaster = false;
            object[] attributes = _windowType.GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                WindowInfo windowInfo = attribute as WindowInfo;
                if(windowInfo == null) continue;
                _windowPath = windowInfo.path;
                _ignoreRaycaster = windowInfo.ignoreRaycast;
                break;
            }
            _assetLoadStatus = AssetLoadStatus.Loading;
            if (_windowPath == null)
            {
                UILog.LogError($"{_windowType.Name}没有配置预制体路径");
                return;
            }
            _parent.assetsAssetLoader.AsyncLoadNInstantiate(_windowPath, OnLoadSuccess, OnLoadFailed);
        }

        private void OnLoadSuccess(GameObject go)
        {
            _assetLoadStatus = AssetLoadStatus.Loaded;
            go.SetActive(false);
            var ui = go.GetComponent(_windowType) as IUIChild;
            if (ui == null)
            {
                UILog.LogError($"预制体上没有找到{_windowType.Name}组件");
                ApplicationManager.instance.DelayedNextFrame(() =>
                {
                    GameObject.Destroy(go); 
                });
                _onLoaded?.Invoke();
                _onLoaded = null;
                return;
            }
            ui.prefabPath = _windowPath;
            if (!_parent.transform)
            {
                UILog.LogError($"{_parent.GetType().Name}已经被销毁");
                ApplicationManager.instance.DelayedNextFrame(() =>
                {
                    GameObject.Destroy(go);
                });
                _onLoaded?.Invoke();
                _onLoaded = null;
                return;
            }
            UIUtils.SetUIChildToParent(ui, _parent);
            UIUtils.InitUI(ui, _ignoreRaycaster, UIManager.instance.canvasRenderMode);
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
        
        private void OnLoadFailed()
        {
            _assetLoadStatus = AssetLoadStatus.Loaded;
            _onLoaded?.Invoke();
            _onLoaded = null;
        }
    }
}