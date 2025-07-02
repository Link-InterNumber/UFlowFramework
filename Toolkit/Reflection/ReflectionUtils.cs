using System;
using System.Reflection;

namespace PowerCellStudio
{
    public static class ReflectionUtils
    {
        #region Instance

        // 获取属性值
        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return prop?.GetValue(obj);
        }

        // 设置属性值
        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            prop?.SetValue(obj, value);
        }

        // 获取字段值
        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return field?.GetValue(obj);
        }

        // 设置字段值
        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            field?.SetValue(obj, value);
        }

        // 调用方法
        public static object InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return method?.Invoke(obj, parameters);
        }

        // 创建实例
        public static object CreateInstance(Type type, params object[] args)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Activator.CreateInstance(type, args);
        }

        #endregion

        #region Generic

        // 创建泛型类型实例
        public static object CreateGenericInstance(Type genericType, Type[] typeArgs, params object[] ctorArgs)
        {
            if (genericType == null) throw new ArgumentNullException(nameof(genericType));
            if (typeArgs == null) throw new ArgumentNullException(nameof(typeArgs));
            var constructedType = genericType.MakeGenericType(typeArgs);
            return Activator.CreateInstance(constructedType, ctorArgs);
        }

        // 调用泛型方法（支持静态和实例方法）
        public static object InvokeGenericMethod(object obj, string methodName, Type[] genericTypes, object[] parameters, bool isStatic = false)
        {
            if (obj == null && !isStatic) throw new ArgumentNullException(nameof(obj));
            var type = isStatic ? (Type)obj : obj.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                if (method.Name == methodName && method.IsGenericMethodDefinition)
                {
                    var genericMethod = method.MakeGenericMethod(genericTypes);
                    return genericMethod.Invoke(isStatic ? null : obj, parameters);
                }
            }
            return null;
        }

        #endregion

        #region Static

        // 调用静态方法
        public static object InvokeStaticMethod(Type type, string methodName, params object[] parameters)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return method?.Invoke(null, parameters);
        }

        // 获取静态属性值
        public static object GetStaticPropertyValue(Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return prop?.GetValue(null);
        }

        // 设置静态属性值
        public static void SetStaticPropertyValue(Type type, string propertyName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            prop?.SetValue(null, value);
        }

        // 获取静态字段值
        public static object GetStaticFieldValue(Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }

        // 设置静态字段值
        public static void SetStaticFieldValue(Type type, string fieldName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value);
        }

        #endregion

    }
}

