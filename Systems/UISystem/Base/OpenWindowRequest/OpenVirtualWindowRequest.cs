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
            // 反射获取windowType的UIVirtualWindow<T>的父类泛型参数T
            Type virtualWindowType = null;
            Type currentType = windowType;
            while (currentType != null)
            {
                var baseType = currentType.BaseType;
                if (baseType == null) break;
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(UIVirtualWindow<>))
                {
                    virtualWindowType = baseType;
                    break;
                }
                currentType = baseType;
            }
            if (virtualWindowType == null)
            {
                UILog.LogError($"{windowType.Name}不是UIVirtualWindow的子类");
                return;
            }
            _bindWindowType = virtualWindowType.GetGenericArguments()[0];
            object[] attributes = _bindWindowType.GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                WindowInfo windowInfo = attribute as WindowInfo;
                if (windowInfo == null) continue;
                path = windowInfo.path;
                ignoreRaycast = windowInfo.ignoreRaycast;
                standaloneCanvas = windowInfo.standaloneCanvas;
                break;
            }
        }

        protected override IUIChild GetWindowInstance(Type windowType, GameObject instanceWindow)
        {
            var windowInstance = instanceWindow.GetComponent(_bindWindowType) as UIWindow;
            if (windowInstance == null)
            {
                UILog.LogError($"{_bindWindowType.Name}没有挂载在预制体上");
                return null;
            }
            var virtualWindowInstance = Activator.CreateInstance(windowType);
            ReflectionUtils.InvokeMethod(virtualWindowInstance, "BindWindow", windowInstance);
            _bindWindowType = null;
            return virtualWindowInstance as IUIChild;
        } 
    }
}