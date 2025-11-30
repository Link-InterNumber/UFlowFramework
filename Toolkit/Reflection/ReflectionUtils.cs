using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    /// <summary>
    /// Reflection utility class for common reflection operations.
    /// 反射工具类，提供常用的反射操作方法。
    /// </summary>
    public static class ReflectionUtils
    {
        #region Create Instance

        public static T Create<T>(params object[] parameters)
        {
            var type = typeof(T);
            return Create<T>(type, parameters);
        }

        public static T Create<T>(Type baseType, params object[] parameters)
        {
            if (baseType.IsAbstract || baseType.IsInterface)
                throw new InvalidOperationException($"Cannot create an instance of abstract class or interface '{baseType.FullName}'.");
            return (T)Activator.CreateInstance(baseType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
        }

        #endregion

        #region Instance

        /// <summary>
        /// Gets the value of a property from an object.
        /// 获取对象的属性值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <returns>The property value. 属性值。</returns>
        public static object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return prop?.GetValue(obj);
        }

        /// <summary>
        /// Sets the value of a property on an object.
        /// 设置对象的属性值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            prop?.SetValue(obj, value);
        }

        /// <summary>
        /// Gets the value of a field from an object.
        /// 获取对象的字段值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <returns>The field value. 字段值。</returns>
        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// Sets the value of a field on an object.
        /// 设置对象的字段值。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// Invokes a method on an object.
        /// 调用对象的方法。
        /// </summary>
        /// <param name="obj">The object instance. 对象实例。</param>
        /// <param name="methodName">The method name. 方法名。</param>
        /// <param name="parameters">The method parameters. 方法参数。</param>
        /// <returns>The return value. 返回值。</returns>
        public static object InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return method?.Invoke(obj, parameters);
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// 创建指定类型的实例。
        /// </summary>
        /// <param name="type">The type to instantiate. 要实例化的类型。</param>
        /// <param name="args">Constructor arguments. 构造函数参数。</param>
        /// <returns>The created instance. 创建的实例。</returns>
        public static object CreateInstance(Type type, params object[] args)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return Activator.CreateInstance(type, args);
        }

        #endregion

        #region Generic

        /// <summary>
        /// Creates an instance of a generic type.
        /// 创建泛型类型的实例。
        /// </summary>
        /// <param name="genericType">The generic type definition. 泛型类型定义。</param>
        /// <param name="typeArgs">The type arguments. 类型参数。</param>
        /// <param name="ctorArgs">Constructor arguments. 构造函数参数。</param>
        /// <returns>The created instance. 创建的实例。</returns>
        public static object CreateGenericInstance(Type genericType, Type[] typeArgs, params object[] ctorArgs)
        {
            if (genericType == null) throw new ArgumentNullException(nameof(genericType));
            if (typeArgs == null) throw new ArgumentNullException(nameof(typeArgs));
            var constructedType = genericType.MakeGenericType(typeArgs);
            return Activator.CreateInstance(constructedType, ctorArgs);
        }

        /// <summary>
        /// Invokes a generic method (supports static and instance methods).
        /// 调用泛型方法（支持静态和实例方法）。
        /// </summary>
        /// <param name="obj">The object instance or type (for static). 对象实例或类型（静态方法时）。</param>
        /// <param name="methodName">The method name. 方法名。</param>
        /// <param name="genericTypes">The generic type arguments. 泛型类型参数。</param>
        /// <param name="parameters">The method parameters. 方法参数。</param>
        /// <param name="isStatic">Is static method. 是否为静态方法。</param>
        /// <returns>The return value. 返回值。</returns>
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

        /// <summary>
        /// Invokes a static method.
        /// 调用静态方法。
        /// </summary>
        /// <param name="type">The type containing the static method. 包含静态方法的类型。</param>
        /// <param name="methodName">The method name. 方法名。</param>
        /// <param name="parameters">The method parameters. 方法参数。</param>
        /// <returns>The return value. 返回值。</returns>
        public static object InvokeStaticMethod(Type type, string methodName, params object[] parameters)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return method?.Invoke(null, parameters);
        }

        /// <summary>
        /// Gets the value of a static property.
        /// 获取静态属性值。
        /// </summary>
        /// <param name="type">The type containing the static property. 包含静态属性的类型。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <returns>The property value. 属性值。</returns>
        public static object GetStaticPropertyValue(Type type, string propertyName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return prop?.GetValue(null);
        }

        /// <summary>
        /// Sets the value of a static property.
        /// 设置静态属性值。
        /// </summary>
        /// <param name="type">The type containing the static property. 包含静态属性的类型。</param>
        /// <param name="propertyName">The property name. 属性名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetStaticPropertyValue(Type type, string propertyName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            prop?.SetValue(null, value);
        }

        /// <summary>
        /// Gets the value of a static field.
        /// 获取静态字段值。
        /// </summary>
        /// <param name="type">The type containing the static field. 包含静态字段的类型。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <returns>The field value. 字段值。</returns>
        public static object GetStaticFieldValue(Type type, string fieldName)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }

        /// <summary>
        /// Sets the value of a static field.
        /// 设置静态字段值。
        /// </summary>
        /// <param name="type">The type containing the static field. 包含静态字段的类型。</param>
        /// <param name="fieldName">The field name. 字段名。</param>
        /// <param name="value">The value to set. 要设置的值。</param>
        public static void SetStaticFieldValue(Type type, string fieldName, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value);
        }

        #endregion

        /// <summary>
        /// Gets all instantiable subclasses (including generic subclasses) of a given type in the specified assembly.
        /// 获取指定类型（包括泛型类型）在指定程序集中的所有可实例化子类。
        /// </summary>
        /// <param name="baseType">The base type or generic type definition. 基类或泛型类型定义。</param>
        /// <param name="assembly">The assembly to search. 要搜索的程序集。</param>
        /// <returns>List of instantiable subclasses. 可实例化子类的列表。</returns>
        public static List<Type> GetInstantiableSubclasses(Type baseType, Assembly assembly = null)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            assembly ??= Assembly.GetAssembly(baseType);
            return assembly.GetTypes()
                .Where(t =>
                    t != baseType &&
                    baseType.IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    (!baseType.IsGenericTypeDefinition || (t.IsGenericType && t.GetGenericTypeDefinition() == baseType))
                )
                .ToList();
        }
    }
}
