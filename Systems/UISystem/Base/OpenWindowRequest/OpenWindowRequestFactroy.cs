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
            else if (IsSubclassOfRawGeneric(typeof(UIVirtualWindow<>), windowType))
            {
                return new OpenVirtualWindowRequest(parent, windowType, preload, sourceData, beforeOpen);
            }
            return null;
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                if (toCheck.IsGenericType && toCheck.GetGenericTypeDefinition() == generic)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}