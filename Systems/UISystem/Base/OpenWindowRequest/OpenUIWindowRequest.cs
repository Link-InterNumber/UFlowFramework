using System;
using UnityEngine;

namespace PowerCellStudio
{
    public class OpenUIWindowRequest : OpenWindowRequestBase
    {
        public OpenUIWindowRequest(IUIParent parent, Type windowType, bool preload, object sourceData, Action beforeOpen)
            : base(parent, windowType, preload, sourceData, beforeOpen)
        {
        }

        protected override void GetWindowInfo(Type windowType, out string path, out bool ignoreRaycast, out bool standaloneCanvas)
        {
            path = null;
            ignoreRaycast = false;
            standaloneCanvas = false;
            object[] attributes = windowType.GetCustomAttributes(true);
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
            return instanceWindow.GetComponent(windowType) as IUIChild;
        }
   }
}