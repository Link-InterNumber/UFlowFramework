using System;

namespace PowerCellStudio
{
    public static class OpenWindowRequestFactroy
    {
        public static IOpenWindowRequest Create(IUIParent parent, Type windowType, bool preload, object sourceData, Action beforeOpen)
        {
            if (typeof(UIWindow).IsAssignableFrom(windowType))
            {
                return new OpenUIWindowRequest(parent, windowType, preload, sourceData, beforeOpen);
            }
            else if(typeof(UIVirtualWindow<>).IsAssignableFrom(windowType))
            {
                return new OpenVirtualWindowRequest(parent, windowType, preload, sourceData, beforeOpen);
            }
            return null;
        }
    }
}