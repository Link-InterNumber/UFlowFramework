using System;
using UnityEngine;

namespace PowerCellStudio
{
    public class OpenVirtualWindowRequest : OpenWindowRequestBase
    {
        public OpenVirtualWindowRequest(IUIParent parent, Type windowType, bool preload, object sourceData, Action beforeOpen)
            : base(parent, windowType, preload, sourceData, beforeOpen)
        {

        }
        
        private Type _bindWindowType;
        protected override void GetWindowInfo(Type windowType, out string path, out bool ignoreRaycast, out bool standaloneCanvas)
        {
            path = null;
            ignoreRaycast = false;
            standaloneCanvas = false;
            _bindWindowType = WindowInfoCache.GetBindWindowType(windowType);
            if (_bindWindowType == null)
                return;
            var windowInfo = WindowInfoCache.GetInfo(_bindWindowType);
            if (windowInfo == null)
                return;
            path = windowInfo.path;
            ignoreRaycast = windowInfo.ignoreRaycast;
            standaloneCanvas = windowInfo.standaloneCanvas;
        }

        protected override IUIChild GetWindowInstance(Type windowType, GameObject instanceWindow)
        {
            var windowInstance = instanceWindow.GetComponent(_bindWindowType) as UIWindow;
            if (windowInstance == null)
            {
                UILogger.LogError($"{_bindWindowType.Name}没有挂载在预制体上");
                return null;
            }
            instanceWindow.name = $"{instanceWindow.name}({windowType.Name})";
            var virtualWindowInstance = Activator.CreateInstance(windowType);
            ReflectionUtils.InvokeMethod(virtualWindowInstance, "BindWindow", windowInstance);
            _bindWindowType = null;
            return virtualWindowInstance as IUIChild;
        } 
    }
}