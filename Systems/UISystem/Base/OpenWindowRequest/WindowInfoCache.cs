using System;
using System.Collections.Concurrent;

namespace PowerCellStudio
{
    public class WindowInfoCache
    {
        private static ConcurrentDictionary<Type, WindowInfo> _windowInfos = new ConcurrentDictionary<Type, WindowInfo>();

        public static WindowInfo GetInfo(Type type)
        {
            if (_windowInfos.TryGetValue(type, out var windowInfo))
            {
                return windowInfo;
            }
            object[] attributes = type.GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                WindowInfo temp = attribute as WindowInfo;
                if (temp == null) continue;
                _windowInfos[type] = temp;
                return temp;
            }
            return null;
        }
        
        private static ConcurrentDictionary<Type, Type> _bindWindowType = new ConcurrentDictionary<Type, Type>();

        public static Type GetBindWindowType(Type windowType)
        {
            if (_bindWindowType.TryGetValue(windowType, out var bindType))
            {
                return bindType;
            }
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
                UILogger.LogError($"{windowType.Name}不是UIVirtualWindow的子类");
                return null;
            }
            var bindWindowType = virtualWindowType.GetGenericArguments()[0];
            _bindWindowType[bindWindowType] = windowType;
            return windowType;
        }
    }
}